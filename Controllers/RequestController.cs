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
public class RequestController : AppBaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly MatchingService _matchingService;
    private readonly PointsService _pointsService;

    public RequestController(AppDbContext context, UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment, MatchingService matchingService, PointsService pointsService)
        : base(context, userManager)
    {
        _userManager = userManager;
        _environment = environment;
        _matchingService = matchingService;
        _pointsService = pointsService;
    }

    public async Task<IActionResult> MyRequests(int? selectedId)
    {
        var userId = _userManager.GetUserId(User)!;

        var myRequests = await _db.ItemRequests
            .Include(r => r.Images)
            .Include(r => r.Feedbacks)
            .Include(r => r.Offers)
                .ThenInclude(o => o.User)
            .Include(r => r.Offers)
                .ThenInclude(o => o.Images)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var selected = selectedId.HasValue
            ? myRequests.FirstOrDefault(r => r.Id == selectedId.Value)
            : null;

        ViewBag.MyRequests = myRequests;
        ViewBag.Selected = selected;

        if (Request.IsAjaxRequest())
        {
            ViewBag.Matches = TempData["Matches"] as string;
            ViewBag.MatchCount = int.TryParse(TempData["MatchCount"] as string, out var mc) ? mc : 0;
            ViewBag.MatchContext = TempData["MatchContext"] as string ?? "";
            return PartialView("~/Views/Profile/Request/RequestSaya.cshtml");
        }

        TempData.Keep("Matches");
        TempData.Keep("MatchCount");
        TempData.Keep("MatchContext");

        ViewBag.InitialSection = "request";
        return View("~/Views/Profile/Shell.cshtml");
    }

    public async Task<IActionResult> Permintaan(int? selectedId)
    {
        var userId = _userManager.GetUserId(User)!;

        var claimRequests = await _db.ClaimRequests
            .Include(r => r.Item)
                .ThenInclude(i => i!.Images)
            .Include(r => r.Item)
                .ThenInclude(i => i!.User)
            .Include(r => r.Feedbacks)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var selected = selectedId.HasValue
            ? claimRequests.FirstOrDefault(r => r.Id == selectedId.Value)
            : null;

        ViewBag.ClaimRequests = claimRequests;
        ViewBag.Selected = selected;
        ViewBag.CurrentUserId = userId;

        if (Request.IsAjaxRequest())
            return PartialView("~/Views/Profile/Request/Permintaan.cshtml");

        ViewBag.InitialSection = "request";
        return View("~/Views/Profile/Shell.cshtml");
    }

    public async Task<IActionResult> CreateView()
    {
        var user = await _userManager.GetUserAsync(User);
        ViewBag.UserAlamat = user?.Alamat ?? "";
        ViewBag.UserProvinsi = user?.Provinsi ?? "";
        ViewBag.UserKota = user?.Kota ?? "";

        var userId = _userManager.GetUserId(User)!;
        var limit = await GetOrCreateRequestLimit(userId);
        ViewBag.RequestsRemaining = RequestLimit.MaxRequestsPerWeek - (limit.IsLimitReached() ? RequestLimit.MaxRequestsPerWeek : limit.RequestCount);
        ViewBag.LimitReached = limit.IsLimitReached();
        ViewBag.PeriodEnd = limit.GetPeriodEnd();

        if (Request.IsAjaxRequest())
            return PartialView("~/Views/Profile/Request/BuatRequest.cshtml");

        ViewBag.InitialSection = "request";
        return View("~/Views/Profile/Shell.cshtml");
    }

    public async Task<IActionResult> EditView(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var itemRequest = await _db.ItemRequests
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (itemRequest == null) return RedirectToAction("MyRequests");

        var user = await _userManager.GetUserAsync(User);
        ViewBag.UserAlamat = user?.Alamat ?? "";
        ViewBag.UserProvinsi = user?.Provinsi ?? "";
        ViewBag.UserKota = user?.Kota ?? "";
        ViewBag.EditRequest = itemRequest;

        if (Request.IsAjaxRequest())
            return PartialView("~/Views/Profile/Request/BuatRequest.cshtml");

        ViewBag.InitialSection = "request";
        return View("~/Views/Profile/Shell.cshtml");
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(ItemCategory? kategori)
    {
        var query = _db.ItemRequests
            .Include(r => r.Images)
            .Include(r => r.User)
            .Include(r => r.Offers)
            .Where(r => r.Status == ItemRequestStatus.Open && r.ExpiresAt > DateTime.UtcNow);

        if (kategori.HasValue)
            query = query.Where(r => r.Kategori == kategori.Value);

        var itemRequests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        ViewBag.SelectedKategori = kategori;
        return View("Request", itemRequests);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Detail(int id)
    {
        var userId = _userManager.GetUserId(User);

        var itemRequest = await _db.ItemRequests
            .Include(r => r.Images)
            .Include(r => r.User)
            .Include(r => r.Offers)
                .ThenInclude(o => o.User)
            .Include(r => r.Offers)
                .ThenInclude(o => o.Images)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (itemRequest == null) return NotFound();

        ViewBag.IsOwner = userId == itemRequest.UserId;
        ViewBag.HasOffered = userId != null && itemRequest.Offers.Any(o => o.UserId == userId);
        ViewBag.CurrentUserId = userId;

        return View("RequestDetail", itemRequest);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RequestFormViewModel form, List<IFormFile> Images)
    {
        if (!ModelState.IsValid)
        {
            TempData["RequestError"] = "Pastikan semua field wajib sudah diisi dengan benar.";
            return RedirectToAction("MyRequests");
        }

        var userId = _userManager.GetUserId(User)!;

        var limit = await GetOrCreateRequestLimit(userId);
        if (limit.IsLimitReached())
        {
            TempData["RequestError"] = $"Anda telah mencapai batas maksimum {RequestLimit.MaxRequestsPerWeek} request per minggu. Silakan coba lagi setelah {limit.GetPeriodEnd():dd MMM yyyy HH:mm} UTC.";
            return RedirectToAction("CreateView");
        }

        if (Images != null && Images.Any(img => !ImageStorage.IsValidImage(img)))
        {
            TempData["RequestError"] = "Format gambar tidak valid. Hanya file JPG/PNG maksimal 5 MB yang diperbolehkan.";
            return RedirectToAction("CreateView");
        }

        var user = await _userManager.FindByIdAsync(userId);

        var itemRequest = new ItemRequest
        {
            Title = form.Title,
            Kategori = form.Kategori,
            KondisiMinimum = form.KondisiMinimum,
            Deskripsi = form.Deskripsi,
            Jumlah = form.Jumlah,
            Provinsi = string.IsNullOrWhiteSpace(form.Provinsi) ? (user?.Provinsi ?? string.Empty) : form.Provinsi!,
            Lokasi = string.IsNullOrWhiteSpace(form.Lokasi) ? (user?.Alamat ?? string.Empty) : form.Lokasi,
            DetailTambahan = string.IsNullOrWhiteSpace(form.DetailTambahanJson) ? null : form.DetailTambahanJson,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(14),
            Status = ItemRequestStatus.Open
        };

        _db.ItemRequests.Add(itemRequest);
        await _db.SaveChangesAsync();

        limit.Increment();
        await _db.SaveChangesAsync();

        if (Images != null && Images.Count > 0)
            await SaveRequestImagesAsync(Images, userId, itemRequest.Id, maxCount: 5);

        await _db.SaveChangesAsync();
        await RunMatchingAndStoreTempData(itemRequest.Id, userId);

        return RedirectToAction("MyRequests", new { selectedId = itemRequest.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int id, RequestFormViewModel form, List<IFormFile>? Images)
    {
        var userId = _userManager.GetUserId(User)!;
        var existing = await _db.ItemRequests
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (existing == null) return RedirectToAction("MyRequests");

        if (!ModelState.IsValid)
        {
            TempData["RequestError"] = "Pastikan semua field wajib sudah diisi dengan benar.";
            return RedirectToAction("EditView", new { id });
        }

        existing.Title = form.Title;
        existing.Kategori = form.Kategori;
        existing.Deskripsi = form.Deskripsi;
        existing.Lokasi = form.Lokasi;
        existing.Provinsi = form.Provinsi ?? string.Empty;
        existing.KondisiMinimum = form.KondisiMinimum;
        existing.Jumlah = form.Jumlah;
        existing.DetailTambahan = string.IsNullOrWhiteSpace(form.DetailTambahanJson) ? null : form.DetailTambahanJson;

        if (Images != null && Images.Count > 0)
        {
            if (Images.Any(img => !ImageStorage.IsValidImage(img)))
            {
                TempData["RequestError"] = "Format gambar tidak valid. Hanya file JPG/PNG maksimal 5 MB yang diperbolehkan.";
                return RedirectToAction("EditView", new { id });
            }
            await SaveRequestImagesAsync(Images, userId, existing.Id, maxCount: 5, currentCount: existing.Images.Count);
        }

        await _db.SaveChangesAsync();
        TempData["RequestSuccess"] = "Request berhasil diperbarui.";
        return RedirectToAction("MyRequests");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRequest(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var itemRequest = await _db.ItemRequests
            .Include(r => r.Images)
            .Include(r => r.Offers)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (itemRequest == null) return RedirectToAction("MyRequests");

        if (itemRequest.Offers.Any(o => o.Status == TransactionStatus.Accepted))
        {
            TempData["RequestError"] = "Request tidak dapat dihapus karena sudah ada penawaran yang diterima.";
            return RedirectToAction("MyRequests");
        }

        foreach (var image in itemRequest.Images)
            DeleteFileIfExists(image.FilePath);

        _db.ItemRequests.Remove(itemRequest);
        await _db.SaveChangesAsync();
        TempData["RequestSuccess"] = "Request berhasil dihapus.";
        return RedirectToAction("MyRequests");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRequestImage(int imageId, int requestId)
    {
        var userId = _userManager.GetUserId(User)!;
        var request = await _db.ItemRequests.FirstOrDefaultAsync(r => r.Id == requestId && r.UserId == userId);
        if (request == null) return RedirectToAction("EditView", new { id = requestId });

        var image = await _db.ItemImages.FirstOrDefaultAsync(img =>
            img.Id == imageId && img.ItemRequestId == requestId && img.OwnerType == ImageOwnerType.Request);

        if (image != null)
        {
            DeleteFileIfExists(image.FilePath);
            _db.ItemImages.Remove(image);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction("EditView", new { id = requestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Offer(int itemRequestId, string deskripsi, int jumlah, string namaBarang, int kondisi, List<IFormFile> Images)
    {
        var userId = _userManager.GetUserId(User)!;

        var itemRequest = await _db.ItemRequests.FindAsync(itemRequestId);
        if (itemRequest == null || itemRequest.UserId == userId || itemRequest.Status != ItemRequestStatus.Open)
            return RedirectToAction("Detail", new { id = itemRequestId });

        var alreadyOffered = await _db.RequestOffers
            .AnyAsync(o => o.ItemRequestId == itemRequestId && o.UserId == userId);

        if (alreadyOffered || string.IsNullOrWhiteSpace(deskripsi))
            return RedirectToAction("Detail", new { id = itemRequestId });

        if (Images != null && Images.Any(img => !ImageStorage.IsValidImage(img)))
        {
            TempData["RequestError"] = "Format gambar tidak valid. Hanya file JPG/PNG maksimal 5 MB yang diperbolehkan.";
            return RedirectToAction("Detail", new { id = itemRequestId });
        }

        var user = await _userManager.FindByIdAsync(userId);
        var lokasi = user?.Kota ?? string.Empty;
        var provinsi = user?.Provinsi ?? string.Empty;

        var clampedJumlah = QuantityHelper.Clamp(jumlah, itemRequest.Jumlah);

        var offer = new RequestOffer
        {
            ItemRequestId = itemRequestId,
            UserId = userId,
            NamaBarang = namaBarang?.Trim() ?? "",
            Kondisi = (ItemCondition)kondisi,
            Lokasi = lokasi,
            Provinsi = provinsi,
            Deskripsi = deskripsi.Trim(),
            Jumlah = clampedJumlah,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.RequestOffers.Add(offer);
        await _db.SaveChangesAsync();

        if (Images != null && Images.Count > 0)
            await SaveOfferImagesAsync(Images, offer.Id, maxCount: 3);

        await _db.SaveChangesAsync();

        var offererName = user.GetFullName();

        _db.Notifications.Add(new Notification
        {
            UserId = itemRequest.UserId,
            Message = $"{offererName} menawarkan \"{namaBarang}\" ({clampedJumlah} pcs) untuk request \"{itemRequest.Title}\" Anda.",
            Type = NotificationType.NewOffer,
            RefId = itemRequestId.ToString(),
            Link = "/Request/MyRequests",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return RedirectToAction("Detail", new { id = itemRequestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptOffer(int offerId)
    {
        var userId = _userManager.GetUserId(User)!;

        var offer = await _db.RequestOffers
            .Include(o => o.ItemRequest)
            .FirstOrDefaultAsync(o => o.Id == offerId);

        if (offer == null || offer.ItemRequest == null || offer.ItemRequest.UserId != userId)
            return RedirectToAction("MyRequests");

        offer.Status = TransactionStatus.Accepted;
        offer.ItemRequest.Status = ItemRequestStatus.Fulfilled;

        _db.Notifications.Add(new Notification
        {
            UserId = offer.UserId,
            Message = $"Penawaran Anda ({offer.Jumlah} pcs) untuk request \"{offer.ItemRequest.Title}\" telah diterima!",
            Type = NotificationType.OfferAccepted,
            RefId = offer.ItemRequestId.ToString(),
            Link = "/Request/Detail/" + offer.ItemRequestId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return RedirectToAction("MyRequests", new { selectedId = offer.ItemRequestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectOffer(int offerId)
    {
        var userId = _userManager.GetUserId(User)!;

        var offer = await _db.RequestOffers
            .Include(o => o.ItemRequest)
            .FirstOrDefaultAsync(o => o.Id == offerId);

        if (offer == null || offer.ItemRequest == null || offer.ItemRequest.UserId != userId)
            return RedirectToAction("MyRequests");

        offer.Status = TransactionStatus.Rejected;

        _db.Notifications.Add(new Notification
        {
            UserId = offer.UserId,
            Message = $"Penawaran Anda untuk request \"{offer.ItemRequest.Title}\" ditolak.",
            Type = NotificationType.OfferRejected,
            RefId = offer.ItemRequestId.ToString(),
            Link = "/Request/Detail/" + offer.ItemRequestId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return RedirectToAction("MyRequests", new { selectedId = offer.ItemRequestId });
    }

    [HttpGet]
    public async Task<IActionResult> FindMatches(int requestId)
    {
        var userId = _userManager.GetUserId(User)!;

        var itemRequest = await _db.ItemRequests
            .Include(r => r.Images)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.UserId == userId);

        if (itemRequest == null)
            return Json(new { success = false, error = "Request tidak ditemukan." });

        var matches = await _matchingService.FindMatchesForItemRequest(itemRequest);

        if (!matches.Any())
            return Json(new { success = true, count = 0, matches = new object[] { } });

        var matchData = matches.Select(m => new
        {
            id = m.Item.Id,
            title = m.Item.NamaBarang,
            kategori = m.Item.Kategori.ToString(),
            lokasi = m.Item.Lokasi,
            provinsi = m.Item.Provinsi,
            deskripsi = m.Item.Deskripsi,
            score = m.Score,
            reasons = m.MatchReasons,
            posterName = m.Item.User != null ? m.Item.User.NamaDepan + " " + m.Item.User.NamaBelakang : "Unknown",
            posterAvatar = m.Item.User?.NamaDepan?.Substring(0, 1).ToUpper() ?? "?",
            createdAgo = (DateTime.UtcNow - m.Item.CreatedAt).Days,
            firstImage = m.Item.Images.FirstOrDefault()?.FilePath,
            type = "item",
            fromRequestId = requestId
        }).ToList();

        return Json(new { success = true, count = matchData.Count, matches = matchData, sourceId = requestId, context = "request" });
    }

    private Task SaveRequestImagesAsync(List<IFormFile> images, string userId, int requestId, int maxCount, int currentCount = 0)
        => ImageStorage.SaveImagesAsync(_db, _environment.WebRootPath, images, maxCount, currentCount,
            img => { img.OwnerType = ImageOwnerType.Request; img.ItemRequestId = requestId; },
            "uploads", "requests", userId, requestId.ToString());

    private Task SaveOfferImagesAsync(List<IFormFile> images, int offerId, int maxCount)
        => ImageStorage.SaveImagesAsync(_db, _environment.WebRootPath, images, maxCount, 0,
            img => { img.OwnerType = ImageOwnerType.RequestOffer; img.RequestOfferId = offerId; },
            "uploads", "offers", offerId.ToString());

    private void DeleteFileIfExists(string relativePath)
        => ImageStorage.DeleteFileIfExists(_environment.WebRootPath, relativePath);

    private async Task<RequestLimit> GetOrCreateRequestLimit(string userId)
    {
        var limit = await _db.RequestLimits.FirstOrDefaultAsync(l => l.UserId == userId);
        if (limit == null)
        {
            limit = new RequestLimit { UserId = userId, RequestCount = 0, PeriodStart = DateTime.UtcNow };
            _db.RequestLimits.Add(limit);
            await _db.SaveChangesAsync();
        }
        return limit;
    }

    private async Task RunMatchingAndStoreTempData(int requestId, string userId)
    {
        var savedRequest = await _db.ItemRequests
            .Include(r => r.Images)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (savedRequest == null) return;

        var matches = await _matchingService.FindMatchesForItemRequest(savedRequest);
        if (!matches.Any()) return;

        var matchData = matches.Select(m => new
        {
            id = m.Item.Id,
            title = m.Item.NamaBarang,
            kategori = m.Item.Kategori.ToString(),
            lokasi = m.Item.Lokasi,
            provinsi = m.Item.Provinsi,
            deskripsi = m.Item.Deskripsi,
            score = m.Score,
            reasons = m.MatchReasons,
            posterName = m.Item.User != null ? m.Item.User.NamaDepan + " " + m.Item.User.NamaBelakang : "Unknown",
            posterAvatar = m.Item.User?.NamaDepan?.Substring(0, 1).ToUpper() ?? "?",
            createdAgo = (DateTime.UtcNow - m.Item.CreatedAt).Days,
            firstImage = m.Item.Images.FirstOrDefault()?.FilePath,
            type = "item",
            fromRequestId = requestId
        }).ToList();

        TempData["Matches"] = JsonSerializer.Serialize(matchData);
        TempData["MatchCount"] = matches.Count.ToString();
        TempData["MatchContext"] = "request";
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChooseOfferDeliveryMethod(int offerId, MetodePengiriman metode)
    {
        var userId = _userManager.GetUserId(User)!;

        var offer = await _db.RequestOffers
            .Include(o => o.ItemRequest)
            .FirstOrDefaultAsync(o => o.Id == offerId && o.ItemRequest!.UserId == userId);

        if (offer == null || offer.ItemRequest == null)
            return RedirectToAction("MyRequests");
        if (offer.Status != TransactionStatus.Accepted)
            return RedirectToAction("MyRequests");
        if (metode == MetodePengiriman.BelumDipilih)
            return RedirectToAction("MyRequests");

        offer.ItemRequest.MetodePengiriman = metode;

        var metodeLabel = metode == MetodePengiriman.Pickup ? "Pickup" : "Kurir";
        _db.Notifications.Add(new Notification
        {
            UserId = offer.UserId,
            Message = $"Peminta memilih metode pengiriman \"{metodeLabel}\" untuk \"{offer.NamaBarang}\".",
            Type = NotificationType.ItemShipped,
            RefId = offerId.ToString(),
            Link = "/Donasi/PenawaranDonasi?selectedId=" + offer.Id,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return RedirectToAction("MyRequests", new { selectedId = offer.ItemRequestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkOfferShipped(int offerId)
    {
        var userId = _userManager.GetUserId(User)!;

        var offer = await _db.RequestOffers
            .Include(o => o.ItemRequest)
            .FirstOrDefaultAsync(o => o.Id == offerId);

        if (offer == null || offer.ItemRequest == null || offer.UserId != userId)
            return RedirectToAction("MyRequests");
        if (offer.Status != TransactionStatus.Accepted)
            return RedirectToAction("MyRequests");
        if (offer.ItemRequest.MetodePengiriman == MetodePengiriman.BelumDipilih)
            return RedirectToAction("MyRequests");

        offer.Status = TransactionStatus.Shipped;

        var shippedMsg = offer.ItemRequest.MetodePengiriman == MetodePengiriman.Pickup
            ? $"Barang \"{offer.NamaBarang}\" ({offer.Jumlah} pcs) untuk request \"{offer.ItemRequest.Title}\" telah di-pickup. Silakan konfirmasi penerimaan."
            : $"Barang \"{offer.NamaBarang}\" ({offer.Jumlah} pcs) untuk request \"{offer.ItemRequest.Title}\" telah dikirim. Silakan konfirmasi penerimaan.";

        _db.Notifications.Add(new Notification
        {
            UserId = offer.ItemRequest.UserId,
            Message = shippedMsg,
            Type = NotificationType.ItemShipped,
            RefId = offerId.ToString(),
            Link = "/Request/MyRequests",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return RedirectToAction("PenawaranDonasi", "Donasi", new { selectedId = offer.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkOfferDelivered(int offerId)
    {
        var userId = _userManager.GetUserId(User)!;

        var offer = await _db.RequestOffers
            .Include(o => o.ItemRequest)
            .FirstOrDefaultAsync(o => o.Id == offerId);

        if (offer == null || offer.ItemRequest == null || offer.ItemRequest.UserId != userId)
            return RedirectToAction("MyRequests");
        if (offer.Status != TransactionStatus.Shipped)
            return RedirectToAction("MyRequests");

        offer.Status = TransactionStatus.Delivered;

        _db.Notifications.Add(new Notification
        {
            UserId = offer.UserId,
            Message = $"Barang \"{offer.NamaBarang}\" ({offer.Jumlah} pcs) untuk request \"{offer.ItemRequest.Title}\" telah dikonfirmasi diterima.",
            Type = NotificationType.ItemDelivered,
            RefId = offer.ItemRequestId.ToString(),
            Link = "/Donasi/PenawaranDonasi",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await _pointsService.AwardPointsAsync(offer.UserId, null, offerId);
        return RedirectToAction("MyRequests", new { selectedId = offer.ItemRequestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Chat(int offerId)
    {
        var userId = _userManager.GetUserId(User)!;

        var offer = await _db.RequestOffers
            .Include(o => o.ItemRequest)
            .FirstOrDefaultAsync(o => o.Id == offerId);

        if (offer == null) return RedirectToAction("MyRequests");

        var isRequester = offer.ItemRequest.UserId == userId;
        var isDonor = offer.UserId == userId;
        if (!isRequester && !isDonor) return RedirectToAction("MyRequests");

        var requesterId = offer.ItemRequest.UserId;
        var donorId = offer.UserId;

        var existing = await _db.Conversations
            .FirstOrDefaultAsync(c => c.RequestOfferId == offerId && c.RequesterId == requesterId && c.DonorId == donorId);

        if (existing != null)
            return RedirectToAction("Index", "Pesan", new { convId = existing.Id });

        var conversation = new Conversation
        {
            Type = ConversationType.RequestOffer,
            RequestOfferId = offerId,
            RequesterId = requesterId,
            DonorId = donorId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();

        return RedirectToAction("Index", "Pesan", new { convId = conversation.Id });
    }
}