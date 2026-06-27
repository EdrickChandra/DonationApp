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
public class DonasiController : AppBaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;
    private readonly MatchingService _matching;
    private readonly PointsService _pointsService;

    public DonasiController(AppDbContext db, UserManager<ApplicationUser> userManager,
     IWebHostEnvironment env, MatchingService matching, PointsService pointsService)
     : base(db, userManager)
    {
        _userManager = userManager;
        _env = env;
        _matching = matching;
        _pointsService = pointsService;
    }

    public async Task<IActionResult> Index(int? selectedId)
    {
        var userId = _userManager.GetUserId(User)!;

        var donations = await _db.Items
            .Include(i => i.Images)
            .Include(i => i.ClaimRequests)
                .ThenInclude(r => r.User)
            .Include(i => i.ClaimRequests)
                .ThenInclude(r => r.Feedbacks)
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        var selected = selectedId.HasValue
            ? donations.FirstOrDefault(d => d.Id == selectedId.Value)
            : null;

        ViewBag.Donations = donations;
        ViewBag.Selected = selected;

        if (Request.IsAjaxRequest())
        {
            ViewBag.Matches = TempData["Matches"] as string;
            ViewBag.MatchCount = int.TryParse(TempData["MatchCount"] as string, out var mc) ? mc : 0;
            ViewBag.MatchContext = TempData["MatchContext"] as string ?? "";
            return PartialView("~/Views/Profile/Donasi/Donasi.cshtml");
        }

        TempData.Keep("Matches");
        TempData.Keep("MatchCount");
        TempData.Keep("MatchContext");

        ViewBag.InitialSection = "donasi";
        return View("~/Views/Profile/Shell.cshtml");
    }

    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        ViewBag.UserProvinsi = user?.Provinsi ?? "";
        ViewBag.UserKota = user?.Kota ?? "";

        if (Request.IsAjaxRequest())
            return PartialView("~/Views/Profile/Donasi/BuatDonasi.cshtml");

        ViewBag.InitialSection = "donasi";
        return View("~/Views/Profile/Shell.cshtml");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var item = await _db.Items.Include(i => i.Images).FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
        if (item == null) return RedirectToAction("Index");

        var user = await _userManager.GetUserAsync(User);
        ViewBag.UserProvinsi = user?.Provinsi ?? "";
        ViewBag.UserKota = user?.Kota ?? "";
        ViewBag.EditItem = item;

        if (Request.IsAjaxRequest())
            return PartialView("~/Views/Profile/Donasi/BuatDonasi.cshtml");

        ViewBag.InitialSection = "donasi";
        return View("~/Views/Profile/Shell.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DonasiFormViewModel form, List<IFormFile> Images)
    {
        if (Images == null || Images.Count == 0)
        {
            TempData["DonasiError"] = "Minimal 1 foto harus diunggah.";
            return RedirectToAction("Create");
        }

        if (Images.Any(img => !ImageStorage.IsValidImage(img)))
        {
            TempData["DonasiError"] = "Format gambar tidak valid. Hanya file JPG/PNG maksimal 5 MB yang diperbolehkan.";
            return RedirectToAction("Create");
        }

        if (!ModelState.IsValid)
        {
            TempData["DonasiError"] = "Pastikan semua field wajib sudah diisi.";
            return RedirectToAction("Create");
        }

        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.FindByIdAsync(userId);

        var item = new Item
        {
            NamaBarang = form.NamaBarang,
            Kategori = form.Kategori,
            Kondisi = form.Kondisi,
            Lokasi = form.Lokasi,
            Provinsi = string.IsNullOrWhiteSpace(form.Provinsi) ? (user?.Provinsi ?? string.Empty) : form.Provinsi!,
            Deskripsi = form.Deskripsi,
            Jumlah = form.Jumlah,
            DetailTambahan = string.IsNullOrWhiteSpace(form.DetailTambahanJson) ? null : form.DetailTambahanJson,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            Status = ItemStatus.Available
        };

        _db.Items.Add(item);
        await _db.SaveChangesAsync();

        await SaveImagesAsync(Images, userId, item.Id, maxCount: 5);
        await _db.SaveChangesAsync();
        await RunMatchingAndStoreTempData(item.Id);

        return RedirectToAction("Index", new { selectedId = item.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int id, DonasiFormViewModel form, List<IFormFile>? Images)
    {
        var userId = _userManager.GetUserId(User)!;
        var existing = await _db.Items.Include(i => i.Images).FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
        if (existing == null) return RedirectToAction("Index");

        if (!ModelState.IsValid)
        {
            TempData["DonasiError"] = "Pastikan semua field wajib sudah diisi.";
            return RedirectToAction("Edit", new { id });
        }

        existing.NamaBarang = form.NamaBarang;
        existing.Kategori = form.Kategori;
        existing.Kondisi = form.Kondisi;
        existing.Lokasi = form.Lokasi;
        existing.Provinsi = form.Provinsi ?? string.Empty;
        existing.Deskripsi = form.Deskripsi;
        existing.Jumlah = form.Jumlah;
        existing.DetailTambahan = string.IsNullOrWhiteSpace(form.DetailTambahanJson) ? null : form.DetailTambahanJson;

        if (Images != null && Images.Count > 0)
        {
            if (Images.Any(img => !ImageStorage.IsValidImage(img)))
            {
                TempData["DonasiError"] = "Format gambar tidak valid. Hanya file JPG/PNG maksimal 5 MB yang diperbolehkan.";
                return RedirectToAction("Edit", new { id });
            }
            await SaveImagesAsync(Images, userId, existing.Id, maxCount: 5, currentCount: existing.Images.Count);
        }

        await _db.SaveChangesAsync();
        TempData["DonasiSuccess"] = "Donasi berhasil diperbarui.";
        return RedirectToAction("Index", new { selectedId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int imageId, int itemId)
    {
        var userId = _userManager.GetUserId(User)!;
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId);
        if (item == null) return RedirectToAction("Edit", new { id = itemId });

        var image = await _db.ItemImages.FirstOrDefaultAsync(img => img.Id == imageId && img.ItemId == itemId);
        if (image != null)
        {
            DeleteFileIfExists(image.FilePath);
            _db.ItemImages.Remove(image);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction("Edit", new { id = itemId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var item = await _db.Items
            .Include(i => i.Images)
            .Include(i => i.ClaimRequests)
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (item == null) return RedirectToAction("Index");

        if (item.ClaimRequests.Any(r => r.Status == TransactionStatus.Accepted || r.Status == TransactionStatus.Shipped))
        {
            TempData["DonasiError"] = "Donasi tidak dapat dihapus karena sedang dalam proses pengiriman.";
            return RedirectToAction("Index");
        }

        foreach (var image in item.Images)
            DeleteFileIfExists(image.FilePath);

        _db.Items.Remove(item);
        await _db.SaveChangesAsync();
        TempData["DonasiSuccess"] = "Donasi berhasil dihapus.";
        return RedirectToAction("Index");
    }

    [AllowAnonymous]
    public async Task<IActionResult> Detail(int id)
    {
        var userId = _userManager.GetUserId(User);

        var item = await _db.Items
            .Include(i => i.Images)
            .Include(i => i.User)
            .Include(i => i.ClaimRequests)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null) return NotFound();

        ViewBag.IsOwner = userId == item.UserId;
        ViewBag.HasRequested = userId != null && item.ClaimRequests.Any(r => r.UserId == userId);

        var totalReviews = await _db.Feedbacks
            .CountAsync(f => f.ReviewedUserId == item.UserId);

        ViewBag.AverageRating = (double)(item.User?.AvgRating ?? 0);
        ViewBag.TotalReviews = totalReviews;

        return View("~/Views/Item/Detail.cshtml", item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Request")]
    public async Task<IActionResult> AjukanPermintaan(int itemId, int jumlah = 1)
    {
        var userId = _userManager.GetUserId(User)!;

        var item = await _db.Items.FindAsync(itemId);
        if (item == null || item.UserId == userId || item.Status != ItemStatus.Available)
            return RedirectToAction("Detail", new { id = itemId });

        var existing = await _db.ClaimRequests.AnyAsync(r => r.ItemId == itemId && r.UserId == userId);
        if (!existing)
        {
            var clampedJumlah = QuantityHelper.Clamp(jumlah, item.Jumlah);

            _db.ClaimRequests.Add(new ClaimRequest
            {
                ItemId = itemId,
                UserId = userId,
                Jumlah = clampedJumlah,
                CreatedAt = DateTime.UtcNow
            });

            var requester = await _userManager.FindByIdAsync(userId);
            var requesterName = requester.GetFullName();

            _db.Notifications.Add(new Notification
            {
                UserId = item.UserId,
                Message = $"{requesterName} meminta {clampedJumlah} pcs barang \"{item.NamaBarang}\" milik Anda.",
                Type = NotificationType.ClaimRequest,
                RefId = itemId.ToString(),
                Link = "/Donasi/Index?selectedId=" + itemId,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        return RedirectToAction("Permintaan", "Request");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickOffer(int itemRequestId, int fromItemId)
    {
        var userId = _userManager.GetUserId(User)!;

        var itemRequest = await _db.ItemRequests.FindAsync(itemRequestId);
        if (itemRequest == null || itemRequest.UserId == userId || itemRequest.Status != ItemRequestStatus.Open)
            return Json(new { success = false, error = "Request tidak valid." });

        var fromItem = await _db.Items
            .Include(i => i.Images)
            .Include(i => i.ClaimRequests)
            .FirstOrDefaultAsync(i => i.Id == fromItemId && i.UserId == userId);
        if (fromItem == null)
            return Json(new { success = false, error = "Item tidak ditemukan." });

        if (fromItem.Status != ItemStatus.Available)
            return Json(new { success = false, error = "Item sudah tidak tersedia." });

        if (fromItem.Jumlah <= 0)
            return Json(new { success = false, error = "Stok item habis." });

        var alreadyOffered = await _db.RequestOffers
            .AnyAsync(o => o.ItemRequestId == itemRequestId && o.UserId == userId);

        if (alreadyOffered)
            return Json(new { success = false, error = "Anda sudah menawarkan untuk request ini." });

        var clampedJumlah = QuantityHelper.Clamp(fromItem.Jumlah, itemRequest.Jumlah);

        var offer = new RequestOffer
        {
            ItemRequestId = itemRequestId,
            UserId = userId,
            NamaBarang = fromItem.NamaBarang,
            Kondisi = fromItem.Kondisi,
            Lokasi = fromItem.Lokasi,
            Provinsi = fromItem.Provinsi,
            Jumlah = clampedJumlah,
            Deskripsi = fromItem.Deskripsi,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.RequestOffers.Add(offer);
        await _db.SaveChangesAsync();

        if (!fromItem.ClaimRequests.Any())
        {
            foreach (var img in fromItem.Images.ToList())
            {
                img.OwnerType = ImageOwnerType.RequestOffer;
                img.ItemId = null;
                img.RequestOfferId = offer.Id;
            }
            _db.Items.Remove(fromItem);
        }

        var offerer = await _userManager.FindByIdAsync(userId);
        var offererName = offerer.GetFullName();

        _db.Notifications.Add(new Notification
        {
            UserId = itemRequest.UserId,
            Message = $"{offererName} menawarkan \"{fromItem.NamaBarang}\" ({clampedJumlah} pcs) untuk request \"{itemRequest.Title}\" Anda.",
            Type = NotificationType.NewOffer,
            RefId = itemRequestId.ToString(),
            Link = "/Request/MyRequests",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickClaim(int itemId, int jumlah = 1)
    {
        var userId = _userManager.GetUserId(User)!;

        var item = await _db.Items.FindAsync(itemId);
        if (item == null || item.UserId == userId || item.Status != ItemStatus.Available)
            return Json(new { success = false, error = "Item tidak valid." });

        var existing = await _db.ClaimRequests.AnyAsync(r => r.ItemId == itemId && r.UserId == userId);
        if (existing)
            return Json(new { success = false, error = "Anda sudah meminta item ini." });

        var clampedJumlah = QuantityHelper.Clamp(jumlah, item.Jumlah);

        _db.ClaimRequests.Add(new ClaimRequest
        {
            ItemId = itemId,
            UserId = userId,
            Jumlah = clampedJumlah,
            CreatedAt = DateTime.UtcNow
        });

        var requester = await _userManager.FindByIdAsync(userId);
        var requesterName = requester.GetFullName();

        _db.Notifications.Add(new Notification
        {
            UserId = item.UserId,
            Message = $"{requesterName} meminta {clampedJumlah} pcs barang \"{item.NamaBarang}\" milik Anda.",
            Type = NotificationType.ClaimRequest,
            RefId = itemId.ToString(),
            Link = "/Donasi/Index?selectedId=" + itemId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Chat(int itemId, string otherUserId)
    {
        var userId = _userManager.GetUserId(User)!;

        var item = await _db.Items.FindAsync(itemId);
        if (item == null) return RedirectToAction("Index", "Home");

        var isOwner = item.UserId == userId;
        var hasRequest = await _db.ClaimRequests.AnyAsync(r => r.ItemId == itemId && r.UserId == userId);

        if (!isOwner && !hasRequest)
            return RedirectToAction("Detail", new { id = itemId });

        var requesterId = isOwner ? otherUserId : userId;
        var donorId = isOwner ? userId : item.UserId;

        var existing = await _db.Conversations
            .FirstOrDefaultAsync(c => c.ItemId == itemId && c.RequesterId == requesterId && c.DonorId == donorId);

        if (existing != null)
            return RedirectToAction("Index", "Pesan", new { convId = existing.Id });

        var claimRequest = await _db.ClaimRequests
            .FirstOrDefaultAsync(r => r.ItemId == itemId && r.UserId == requesterId);

        if (claimRequest == null)
            return RedirectToAction("Detail", new { id = itemId });

        var conversation = new Conversation
        {
            Type = ConversationType.Donation,
            ItemId = itemId,
            ClaimRequestId = claimRequest.Id,
            RequesterId = requesterId,
            DonorId = donorId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();

        return RedirectToAction("Index", "Pesan", new { convId = conversation.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int requestId)
    {
        var userId = _userManager.GetUserId(User)!;

        var request = await _db.ClaimRequests
            .Include(r => r.Item)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null || request.Item == null || request.Item.UserId != userId)
            return RedirectToAction("Index");

        if (request.Item.Jumlah <= 0)
        {
            TempData["DonasiError"] = "Stok barang sudah habis.";
            return RedirectToAction("Index");
        }

        request.Status = TransactionStatus.Accepted;
        request.UpdatedAt = DateTime.UtcNow;

        request.Item.Jumlah -= request.Jumlah;
        if (request.Item.Jumlah <= 0)
        {
            request.Item.Jumlah = 0;
            request.Item.Status = ItemStatus.Claimed;
        }

        _db.Notifications.Add(new Notification
        {
            UserId = request.UserId,
            Message = $"Permintaan Anda untuk {request.Jumlah} pcs \"{request.Item.NamaBarang}\" diterima!",
            Type = NotificationType.ClaimAccepted,
            RefId = request.ItemId.ToString(),
            Link = "/Request/Permintaan",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return RedirectToAction("Index", new { selectedId = request.ItemId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int requestId)
    {
        var userId = _userManager.GetUserId(User)!;

        var request = await _db.ClaimRequests
            .Include(r => r.Item)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null || request.Item == null || request.Item.UserId != userId)
            return RedirectToAction("Index");

        request.Status = TransactionStatus.Rejected;

        _db.Notifications.Add(new Notification
        {
            UserId = request.UserId,
            Message = $"Permintaan Anda untuk barang \"{request.Item.NamaBarang}\" ditolak.",
            Type = NotificationType.ClaimRejected,
            RefId = request.ItemId.ToString(),
            Link = "/Home/Index",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return RedirectToAction("Index", new { selectedId = request.ItemId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChooseDeliveryMethod(int claimRequestId, MetodePengiriman metode)
    {
        var userId = _userManager.GetUserId(User)!;

        var request = await _db.ClaimRequests
            .Include(r => r.Item)
            .FirstOrDefaultAsync(r => r.Id == claimRequestId && r.UserId == userId);

        if (request == null || request.Item == null)
            return RedirectToAction("Permintaan", "Request");
        if (request.Status != TransactionStatus.Accepted)
            return RedirectToAction("Permintaan", "Request");
        if (metode == MetodePengiriman.BelumDipilih)
            return RedirectToAction("Permintaan", "Request");

        request.MetodePengiriman = metode;
        request.UpdatedAt = DateTime.UtcNow;

        var metodeLabel = metode == MetodePengiriman.Pickup ? "Pickup" : "Kurir";
        _db.Notifications.Add(new Notification
        {
            UserId = request.Item.UserId,
            Message = $"Penerima memilih metode pengiriman \"{metodeLabel}\" untuk \"{request.Item.NamaBarang}\".",
            Type = NotificationType.ItemShipped,
            RefId = claimRequestId.ToString(),
            Link = "/Donasi/Index?selectedId=" + request.Item.Id,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return RedirectToAction("Permintaan", "Request", new { selectedId = request.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkShipped(int claimRequestId)
    {
        var userId = _userManager.GetUserId(User)!;

        var request = await _db.ClaimRequests
            .Include(r => r.Item)
            .FirstOrDefaultAsync(r => r.Id == claimRequestId);

        if (request == null || request.Item == null || request.Item.UserId != userId)
            return RedirectToAction("Index");
        if (request.Status != TransactionStatus.Accepted)
            return RedirectToAction("Index");
        if (request.MetodePengiriman == MetodePengiriman.BelumDipilih)
            return RedirectToAction("Index");

        request.Status = TransactionStatus.Shipped;
        request.UpdatedAt = DateTime.UtcNow;

        var shippedMsg = request.MetodePengiriman == MetodePengiriman.Pickup
            ? $"Barang \"{request.Item.NamaBarang}\" ({request.Jumlah} pcs) telah di-pickup. Silakan konfirmasi penerimaan."
            : $"Barang \"{request.Item.NamaBarang}\" ({request.Jumlah} pcs) telah dikirim. Silakan konfirmasi penerimaan.";

        _db.Notifications.Add(new Notification
        {
            UserId = request.UserId,
            Message = shippedMsg,
            Type = NotificationType.ItemShipped,
            RefId = claimRequestId.ToString(),
            Link = "/Request/Permintaan",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return RedirectToAction("Index", new { selectedId = request.ItemId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDelivered(int claimRequestId)
    {
        var userId = _userManager.GetUserId(User)!;

        var request = await _db.ClaimRequests
            .Include(r => r.Item)
            .FirstOrDefaultAsync(r => r.Id == claimRequestId);

        if (request == null || request.Item == null || request.UserId != userId)
            return RedirectToAction("Permintaan", "Request");
        if (request.Status != TransactionStatus.Shipped)
            return RedirectToAction("Permintaan", "Request");

        request.Status = TransactionStatus.Delivered;
        request.UpdatedAt = DateTime.UtcNow;

        _db.Notifications.Add(new Notification
        {
            UserId = request.Item.UserId,
            Message = $"Barang \"{request.Item.NamaBarang}\" ({request.Jumlah} pcs) telah dikonfirmasi diterima.",
            Type = NotificationType.ItemDelivered,
            RefId = claimRequestId.ToString(),
            Link = "/Donasi/Index",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await _pointsService.AwardPointsAsync(request.Item.UserId, claimRequestId, null);
        return RedirectToAction("Permintaan", "Request", new { selectedId = request.Id });
    }

    [HttpGet]
    public async Task<IActionResult> FindMatches(int itemId)
    {
        var userId = _userManager.GetUserId(User)!;

        var item = await _db.Items
            .Include(i => i.Images)
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId);

        if (item == null)
            return Json(new { success = false, error = "Item tidak ditemukan." });

        var matches = await _matching.FindMatchesForItem(item);

        if (!matches.Any())
            return Json(new { success = true, count = 0, matches = new object[] { } });

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

        return Json(new { success = true, count = matchData.Count, matches = matchData, sourceId = itemId, context = "donasi" });
    }

    private Task SaveImagesAsync(List<IFormFile> images, string userId, int itemId, int maxCount, int currentCount = 0)
        => ImageStorage.SaveImagesAsync(_db, _env.WebRootPath, images, maxCount, currentCount,
            img => { img.OwnerType = ImageOwnerType.Donation; img.ItemId = itemId; },
            "uploads", userId, itemId.ToString());

    private void DeleteFileIfExists(string relativePath)
        => ImageStorage.DeleteFileIfExists(_env.WebRootPath, relativePath);

    private async Task RunMatchingAndStoreTempData(int itemId)
    {
        var savedItem = await _db.Items
            .Include(i => i.Images)
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (savedItem == null) return;

        var matches = await _matching.FindMatchesForItem(savedItem);
        if (!matches.Any()) return;

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

    [HttpGet]
    public async Task<IActionResult> PenawaranDonasi(int? selectedId)
    {
        var userId = _userManager.GetUserId(User)!;

        var offers = await _db.RequestOffers
            .Include(o => o.ItemRequest)
                .ThenInclude(r => r!.Images)
            .Include(o => o.ItemRequest)
                .ThenInclude(r => r!.User)
            .Include(o => o.ItemRequest)
                .ThenInclude(r => r!.Feedbacks)
            .Include(o => o.Images)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var selected = selectedId.HasValue
            ? offers.FirstOrDefault(o => o.Id == selectedId.Value)
            : null;

        ViewBag.MyOffers = offers;
        ViewBag.Selected = selected;

        if (Request.IsAjaxRequest())
            return PartialView("~/Views/Profile/Donasi/PenawaranDonasi.cshtml");

        ViewBag.InitialSection = "donasi";
        return View("~/Views/Profile/Shell.cshtml");
    }
}