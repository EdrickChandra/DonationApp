using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;
using DonationApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace DonationApp.Controllers;

[Authorize]
public class ItemController : BaseController
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly MatchingService _matchingService;

    public ItemController(AppDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment, MatchingService matchingService)
        : base(context, userManager)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
        _matchingService = matchingService;
    }

    public IActionResult Create()
    {
        return View();
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
            return RedirectToAction("Index", "Donasi");
        }

        if (!ModelState.IsValid)
        {
            TempData["DonasiError"] = "Pastikan semua field wajib sudah diisi dengan benar.";
            return RedirectToAction("Index", "Donasi");
        }

        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.FindByIdAsync(userId);

        item.CreatedAt = DateTime.UtcNow;
        item.ExpiresAt = DateTime.UtcNow.AddDays(7);
        item.Status = ItemStatus.Available;
        item.UserId = userId;
        item.Provinsi = user?.Provinsi ?? string.Empty;
        item.DetailTambahan = string.IsNullOrWhiteSpace(DetailTambahanJson) ? null : DetailTambahanJson;

        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", userId, item.Id.ToString());
        Directory.CreateDirectory(uploadFolder);

        int imageCount = 0;
        foreach (var image in Images)
        {
            if (imageCount >= 5) break;

            var extension = Path.GetExtension(image.FileName).ToLower();
            if (!allowedExtensions.Contains(extension)) continue;
            if (image.Length > 5 * 1024 * 1024) continue;

            var fileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(stream);

            _context.ItemImages.Add(new ItemImage
            {
                ItemId = item.Id,
                FilePath = $"/uploads/{userId}/{item.Id}/{fileName}"
            });

            imageCount++;
        }

        await _context.SaveChangesAsync();

        var savedItem = await _context.Items
            .Include(i => i.Images)
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == item.Id);

        if (savedItem != null)
        {
            var matches = await _matchingService.FindMatchesForItem(savedItem);
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
                    posterName = m.ItemRequest.User != null
                        ? m.ItemRequest.User.NamaDepan + " " + m.ItemRequest.User.NamaBelakang
                        : "Unknown",
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

        return RedirectToAction("Index", "Donasi");
    }

    [AllowAnonymous]
    public async Task<IActionResult> Detail(int id)
    {
        var userId = _userManager.GetUserId(User);

        var item = await _context.Items
            .Include(i => i.Images)
            .Include(i => i.ClaimRequests)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null)
            return NotFound();

        ViewBag.IsOwner = userId == item.UserId;
        ViewBag.HasRequested = userId != null && item.ClaimRequests.Any(r => r.UserId == userId);

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Request(int itemId)
    {
        var userId = _userManager.GetUserId(User)!;

        var item = await _context.Items.FindAsync(itemId);
        if (item == null || item.UserId == userId || item.Status != ItemStatus.Available)
            return RedirectToAction("Detail", new { id = itemId });

        var existing = await _context.ClaimRequests
            .AnyAsync(r => r.ItemId == itemId && r.UserId == userId);

        if (!existing)
        {
            var claimRequest = new ClaimRequest
            {
                ItemId = itemId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ClaimRequests.Add(claimRequest);

            var requester = await _userManager.FindByIdAsync(userId);
            var requesterName = requester != null ? requester.NamaDepan + " " + requester.NamaBelakang : "Seseorang";

            _context.Notifications.Add(new Notification
            {
                UserId = item.UserId,
                Message = $"{requesterName} meminta barang \"{item.NamaBarang}\" milik Anda.",
                Link = "/Donasi/Index?selectedId=" + itemId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Detail", new { id = itemId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Chat(int itemId, string otherUserId)
    {
        var userId = _userManager.GetUserId(User)!;

        var item = await _context.Items.FindAsync(itemId);
        if (item == null)
            return RedirectToAction("Index", "Home");

        var isOwner = item.UserId == userId;
        var hasRequest = await _context.ClaimRequests
            .AnyAsync(r => r.ItemId == itemId && r.UserId == userId);

        if (!isOwner && !hasRequest)
            return RedirectToAction("Detail", new { id = itemId });

        var requesterId = isOwner ? otherUserId : userId;
        var donorId = isOwner ? userId : item.UserId;

        var existing = await _context.Conversations
            .FirstOrDefaultAsync(c => c.ItemId == itemId &&
                c.RequesterId == requesterId &&
                c.DonorId == donorId);

        if (existing != null)
            return RedirectToAction("Index", "Pesan", new { convId = existing.Id });

        var claimRequest = await _context.ClaimRequests
            .FirstOrDefaultAsync(r => r.ItemId == itemId && r.UserId == requesterId);

        if (claimRequest == null)
            return RedirectToAction("Detail", new { id = itemId });

        var conversation = new Conversation
        {
            ItemId = itemId,
            ClaimRequestId = claimRequest.Id,
            RequesterId = requesterId,
            DonorId = donorId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Pesan", new { convId = conversation.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkShipped(int claimRequestId)
    {
        var userId = _userManager.GetUserId(User)!;

        var request = await _context.ClaimRequests
            .Include(r => r.Item)
            .FirstOrDefaultAsync(r => r.Id == claimRequestId);

        if (request == null || request.Item == null || request.Item.UserId != userId)
            return RedirectToAction("Index", "Donasi");

        if (request.Status != ClaimRequestStatus.Accepted)
            return RedirectToAction("Index", "Donasi");

        request.Status = ClaimRequestStatus.Shipped;
        request.UpdatedAt = DateTime.UtcNow;

        _context.Notifications.Add(new Notification
        {
            UserId = request.UserId,
            Message = $"Barang \"{request.Item.NamaBarang}\" telah dikirim oleh donor. Silakan konfirmasi penerimaan.",
            Link = "/Request/Permintaan",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return RedirectToAction("Index", "Donasi");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDelivered(int claimRequestId)
    {
        var userId = _userManager.GetUserId(User)!;

        var request = await _context.ClaimRequests
            .Include(r => r.Item)
            .FirstOrDefaultAsync(r => r.Id == claimRequestId);

        if (request == null || request.Item == null || request.UserId != userId)
            return RedirectToAction("MyRequests", "Request");

        if (request.Status != ClaimRequestStatus.Shipped)
            return RedirectToAction("MyRequests", "Request");

        request.Status = ClaimRequestStatus.Delivered;
        request.UpdatedAt = DateTime.UtcNow;

        _context.Notifications.Add(new Notification
        {
            UserId = request.Item.UserId,
            Message = $"Barang \"{request.Item.NamaBarang}\" telah dikonfirmasi diterima oleh penerima.",
            Link = "/Donasi/Index",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return RedirectToAction("Permintaan", "Request");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int requestId)
    {
        var userId = _userManager.GetUserId(User)!;

        var request = await _context.ClaimRequests
            .Include(r => r.Item)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null || request.Item == null || request.Item.UserId != userId)
            return RedirectToAction("Index", "Donasi");

        request.Status = ClaimRequestStatus.Rejected;

        _context.Notifications.Add(new Notification
        {
            UserId = request.UserId,
            Message = $"Permintaan Anda untuk barang \"{request.Item.NamaBarang}\" ditolak.",
            Link = "/Home/Index",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return RedirectToAction("Index", "Donasi");
    }
}