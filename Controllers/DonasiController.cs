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
            : null;

        ViewBag.Donations = donations;
        ViewBag.Selected = selected;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            ViewBag.Matches = TempData["Matches"] as string;
            ViewBag.MatchCount = int.TryParse(TempData["MatchCount"] as string, out var mc) ? mc : 0;
            ViewBag.MatchContext = TempData["MatchContext"] as string ?? "";
            return PartialView("~/Views/Profile/Donasi/Donasi.cshtml");
        }

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

    public async Task<IActionResult> Edit(int id)
    {
        var userId = _um.GetUserId(User)!;
        var item = await _db.Items.Include(i => i.Images).FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
        if (item == null) return RedirectToAction("Index");

        ViewBag.EditItem = item;

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
        if (string.IsNullOrWhiteSpace(item.Provinsi))
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int id, Item item, List<IFormFile>? Images, string? DetailTambahanJson)
    {
        var userId = _um.GetUserId(User)!;
        var existing = await _db.Items.Include(i => i.Images).FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
        if (existing == null) return RedirectToAction("Index");

        ModelState.Remove("UserId");
        ModelState.Remove("User");
        ModelState.Remove("Images");

        if (!ModelState.IsValid)
        {
            TempData["DonasiError"] = "Pastikan semua field wajib sudah diisi.";
            return RedirectToAction("Edit", new { id });
        }

        existing.NamaBarang = item.NamaBarang;
        existing.Kategori = item.Kategori;
        existing.Kondisi = item.Kondisi;
        existing.Lokasi = item.Lokasi;
        existing.Provinsi = item.Provinsi;
        existing.Deskripsi = item.Deskripsi;
        existing.Jumlah = item.Jumlah;
        existing.DetailTambahan = string.IsNullOrWhiteSpace(DetailTambahanJson) ? null : DetailTambahanJson;

        if (Images != null && Images.Count > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", userId, existing.Id.ToString());
            Directory.CreateDirectory(uploadFolder);

            int imageCount = existing.Images.Count;
            foreach (var image in Images)
            {
                if (imageCount >= 5) break;
                var ext = Path.GetExtension(image.FileName).ToLower();
                if (!allowedExtensions.Contains(ext) || image.Length > 5 * 1024 * 1024) continue;
                var fileName = Guid.NewGuid() + ext;
                var filePath = Path.Combine(uploadFolder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await image.CopyToAsync(stream);
                _db.ItemImages.Add(new ItemImage { ItemId = existing.Id, FilePath = $"/uploads/{userId}/{existing.Id}/{fileName}" });
                imageCount++;
            }
        }

        await _db.SaveChangesAsync();
        TempData["DonasiSuccess"] = "Donasi berhasil diperbarui.";
        return RedirectToAction("Index", new { selectedId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int imageId, int itemId)
    {
        var userId = _um.GetUserId(User)!;
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId);
        if (item == null) return RedirectToAction("Edit", new { id = itemId });

        var image = await _db.ItemImages.FirstOrDefaultAsync(img => img.Id == imageId && img.ItemId == itemId);
        if (image != null)
        {
            var fullPath = Path.Combine(_env.WebRootPath, image.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
            _db.ItemImages.Remove(image);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction("Edit", new { id = itemId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _um.GetUserId(User)!;
        var item = await _db.Items
            .Include(i => i.Images)
            .Include(i => i.ClaimRequests)
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (item == null) return RedirectToAction("Index");

        if (item.ClaimRequests.Any(r => r.Status == ClaimRequestStatus.Accepted || r.Status == ClaimRequestStatus.Shipped))
        {
            TempData["DonasiError"] = "Donasi tidak dapat dihapus karena sedang dalam proses pengiriman.";
            return RedirectToAction("Index");
        }

        foreach (var image in item.Images)
        {
            var fullPath = Path.Combine(_env.WebRootPath, image.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }

        _db.Items.Remove(item);
        await _db.SaveChangesAsync();
        TempData["DonasiSuccess"] = "Donasi berhasil dihapus.";
        return RedirectToAction("Index");
    }
}