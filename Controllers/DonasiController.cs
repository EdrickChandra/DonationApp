using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;
using DonationApp.Services;
using System.Text.Json;

namespace DonationApp.Controllers;

[Authorize]
public class DonasiController : ProfileBaseController
{
    private readonly UserManager<ApplicationUser> _um;
    private readonly IWebHostEnvironment _env;
    private readonly MatchingService _matching;

    public DonasiController(AppDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env, MatchingService matching)
        : base(db, userManager)
    {
        _um = userManager;
        _env = env;
        _matching = matching;
    }

    public async Task<IActionResult> Index(int? selectedId)
    {
        var userId = _um.GetUserId(User)!;

        var donations = await _db.Items
            .Include(i => i.Images)
            .Include(i => i.ClaimRequests)
                .ThenInclude(r => r.User)
            .Include(i => i.ClaimRequests)
                .ThenInclude(r => r.Reputations)
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        var selected = selectedId.HasValue
            ? donations.FirstOrDefault(d => d.Id == selectedId.Value)
            : donations.FirstOrDefault(d => d.Status != ItemStatus.Available || d.ExpiresAt > DateTime.UtcNow);

        ViewBag.Donations = donations;
        ViewBag.Selected = selected;
        ViewBag.Matches = TempData["Matches"] as string;
        ViewBag.MatchCount = int.TryParse(TempData["MatchCount"] as string, out var mc) ? mc : 0;
        ViewBag.MatchContext = TempData["MatchContext"] as string ?? "";

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("~/Views/Profile/Donasi/Donasi.cshtml");

        ViewBag.InitialSection = "donasi";
        return View("~/Views/Profile/Shell.cshtml");
    }

    public IActionResult Create()
    {
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("~/Views/Profile/Donasi/BuatDonasi.cshtml");

        ViewBag.InitialSection = "donasi";
        return View("~/Views/Profile/Shell.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Item item, List<IFormFile> Images, string? DetailTambahanJson)
    {
        ModelState.Remove("UserId");
        ModelState.Remove("User");
        ModelState.Remove("Images");

        if (Images == null || Images.Count == 0)
        {
            TempData["DonasiError"] = "Minimal 1 foto harus diunggah.";
            return RedirectToAction("Create");
        }

        if (!ModelState.IsValid)
        {
            TempData["DonasiError"] = "Pastikan semua field wajib sudah diisi.";
            return RedirectToAction("Create");
        }

        var userId = _um.GetUserId(User)!;
        var user = await _um.FindByIdAsync(userId);

        item.UserId = userId;
        item.CreatedAt = DateTime.UtcNow;
        item.ExpiresAt = DateTime.UtcNow.AddDays(7);
        item.Status = ItemStatus.Available;
        item.Provinsi = user?.Provinsi ?? string.Empty;
        item.DetailTambahan = string.IsNullOrWhiteSpace(DetailTambahanJson) ? null : DetailTambahanJson;

        _db.Items.Add(item);
        await _db.SaveChangesAsync();

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", userId, item.Id.ToString());
        Directory.CreateDirectory(uploadFolder);

        int imageCount = 0;
        foreach (var image in Images)
        {
            if (imageCount >= 5) break;
            var ext = Path.GetExtension(image.FileName).ToLower();
            if (!allowedExtensions.Contains(ext) || image.Length > 5 * 1024 * 1024) continue;
            var fileName = Guid.NewGuid() + ext;
            var filePath = Path.Combine(uploadFolder, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(stream);
            _db.ItemImages.Add(new ItemImage { ItemId = item.Id, FilePath = $"/uploads/{userId}/{item.Id}/{fileName}" });
            imageCount++;
        }

        await _db.SaveChangesAsync();

        var savedItem = await _db.Items
            .Include(i => i.Images)
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == item.Id);

        if (savedItem != null)
        {
            var matches = await _matching.FindMatchesForItem(savedItem);
            if (matches.Any())
            {
                var matchData = matches.Select(m => new
                {
                    id = m.ItemRequest.Id,
                    title = m.ItemRequest.Title,
                    kategori = m.ItemRequest.Kategori.ToString(),
                    lokasi = m.ItemRequest.Lokasi,
                    provinsi = m.ItemRequest.Provinsi,
                    deskripsi = m.ItemRequest.Deskripsi,
                    score = m.Score,
                    reasons = m.MatchReasons,
                    posterName = m.ItemRequest.User != null ? m.ItemRequest.User.NamaDepan + " " + m.ItemRequest.User.NamaBelakang : "Unknown",
                    posterAvatar = m.ItemRequest.User?.NamaDepan?.Substring(0, 1).ToUpper() ?? "?",
                    createdAgo = (DateTime.UtcNow - m.ItemRequest.CreatedAt).Days,
                    firstImage = m.ItemRequest.Images.FirstOrDefault()?.FilePath,
                    type = "request"
                }).ToList();
                TempData["Matches"] = JsonSerializer.Serialize(matchData);
                TempData["MatchCount"] = matches.Count.ToString();
                TempData["MatchContext"] = "donasi";
            }
        }

        return RedirectToAction("Index");
    }
}
