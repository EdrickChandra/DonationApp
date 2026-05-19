using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;
using DonationApp.Services;
using System.Text.Json;

namespace DonationApp.Controllers;

public class RequestController : ProfileBaseController
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly MatchingService _matchingService;

    public RequestController(AppDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment, MatchingService matchingService)
        : base(context, userManager)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
        _matchingService = matchingService;
    }

    [Authorize]
    public async Task<IActionResult> MyRequests()
    {
        var userId = _userManager.GetUserId(User)!;

        var myRequests = await _context.ItemRequests
            .Include(r => r.Images)
            .Include(r => r.Offers)
                .ThenInclude(o => o.User)
            .Include(r => r.Offers)
                .ThenInclude(o => o.Images)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        ViewBag.MyRequests = myRequests;
        ViewBag.Matches = TempData["Matches"] as string;
        ViewBag.MatchCount = int.TryParse(TempData["MatchCount"] as string, out var mc) ? mc : 0;
        ViewBag.MatchContext = TempData["MatchContext"] as string ?? "";

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("~/Views/Profile/Request/RequestSaya.cshtml");

        ViewBag.InitialSection = "request";
        return View("~/Views/Profile/Shell.cshtml");
    }

    [Authorize]
    public async Task<IActionResult> Permintaan(int? selectedId)
    {
        var userId = _userManager.GetUserId(User)!;

        var claimRequests = await _context.ClaimRequests
            .Include(r => r.Item)
                .ThenInclude(i => i!.Images)
            .Include(r => r.Item)
                .ThenInclude(i => i!.User)
            .Include(r => r.Reputations)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var selected = selectedId.HasValue
            ? claimRequests.FirstOrDefault(r => r.Id == selectedId.Value)
            : claimRequests.FirstOrDefault();

        ViewBag.ClaimRequests = claimRequests;
        ViewBag.Selected = selected;
        ViewBag.CurrentUserId = userId;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("~/Views/Profile/Request/Permintaan.cshtml");

        ViewBag.InitialSection = "request";
        return View("~/Views/Profile/Shell.cshtml");
    }

    [Authorize]
    public async Task<IActionResult> CreateView()
    {
        var user = await _userManager.GetUserAsync(User);
        ViewBag.UserAlamat = user?.Alamat ?? "";
        ViewBag.UserProvinsi = user?.Provinsi ?? "";

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("~/Views/Profile/Request/BuatRequest.cshtml");

        ViewBag.InitialSection = "request";
        return View("~/Views/Profile/Shell.cshtml");
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(ItemCategory? kategori)
    {
        var query = _context.ItemRequests
            .Include(r => r.Images)
            .Include(r => r.User)
            .Include(r => r.Offers)
            .Where(r => r.Status == ItemRequestStatus.Open && r.ExpiresAt > DateTime.UtcNow);

        if (kategori.HasValue && kategori.Value != ItemCategory.Semua)
            query = query.Where(r => r.Kategori == kategori.Value);

        var itemRequests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

        ViewBag.SelectedKategori = kategori ?? ItemCategory.Semua;

        return View("Request", itemRequests);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Detail(int id)
    {
        var userId = _userManager.GetUserId(User);

        var itemRequest = await _context.ItemRequests
            .Include(r => r.Images)
            .Include(r => r.User)
            .Include(r => r.Offers)
                .ThenInclude(o => o.User)
            .Include(r => r.Offers)
                .ThenInclude(o => o.Images)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (itemRequest == null)
            return NotFound();

        ViewBag.IsOwner = userId == itemRequest.UserId;
        ViewBag.HasOffered = userId != null && itemRequest.Offers.Any(o => o.UserId == userId);
        ViewBag.CurrentUserId = userId;

        return View("RequestDetail", itemRequest);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ItemRequest itemRequest, List<IFormFile> Images, string? DetailTambahanJson)
    {
        ModelState.Remove("UserId");
        ModelState.Remove("User");
        ModelState.Remove("Images");
        ModelState.Remove("Offers");

        if (!ModelState.IsValid)
        {
            TempData["RequestError"] = "Pastikan semua field wajib sudah diisi dengan benar.";
            return RedirectToAction("MyRequests", "Request");
        }

        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.FindByIdAsync(userId);

        itemRequest.UserId = userId;
        itemRequest.CreatedAt = DateTime.UtcNow;
        itemRequest.ExpiresAt = DateTime.UtcNow.AddDays(14);
        itemRequest.Status = ItemRequestStatus.Open;
        itemRequest.Provinsi = string.IsNullOrWhiteSpace(itemRequest.Provinsi)
            ? (user?.Provinsi ?? string.Empty)
            : itemRequest.Provinsi;
        itemRequest.Lokasi = string.IsNullOrWhiteSpace(itemRequest.Lokasi)
            ? (user?.Alamat ?? string.Empty)
            : itemRequest.Lokasi;
        itemRequest.DetailTambahan = string.IsNullOrWhiteSpace(DetailTambahanJson) ? null : DetailTambahanJson;

        _context.ItemRequests.Add(itemRequest);
        await _context.SaveChangesAsync();

        if (Images != null && Images.Count > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "requests", userId, itemRequest.Id.ToString());
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
                _context.RequestImages.Add(new RequestImage
                {
                    ItemRequestId = itemRequest.Id,
                    FilePath = $"/uploads/requests/{userId}/{itemRequest.Id}/{fileName}"
                });
                imageCount++;
            }

            await _context.SaveChangesAsync();
        }

        var savedRequest = await _context.ItemRequests
            .Include(r => r.Images)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == itemRequest.Id);

        if (savedRequest != null)
        {
            var matches = await _matchingService.FindMatchesForItemRequest(savedRequest);
            if (matches.Any())
            {
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
                    type = "item"
                }).ToList();

                TempData["Matches"] = JsonSerializer.Serialize(matchData);
                TempData["MatchCount"] = matches.Count.ToString();
                TempData["MatchContext"] = "request";
            }
        }

        return RedirectToAction("MyRequests", "Request");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Offer(int itemRequestId, string deskripsi, List<IFormFile> Images)
    {
        var userId = _userManager.GetUserId(User)!;

        var itemRequest = await _context.ItemRequests.FindAsync(itemRequestId);
        if (itemRequest == null || itemRequest.UserId == userId || itemRequest.Status != ItemRequestStatus.Open)
            return RedirectToAction("Detail", new { id = itemRequestId });

        var alreadyOffered = await _context.RequestOffers
            .AnyAsync(o => o.ItemRequestId == itemRequestId && o.UserId == userId);

        if (alreadyOffered)
            return RedirectToAction("Detail", new { id = itemRequestId });

        if (string.IsNullOrWhiteSpace(deskripsi))
            return RedirectToAction("Detail", new { id = itemRequestId });

        var offer = new RequestOffer
        {
            ItemRequestId = itemRequestId,
            UserId = userId,
            Deskripsi = deskripsi.Trim(),
            Status = RequestOfferStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.RequestOffers.Add(offer);
        await _context.SaveChangesAsync();

        if (Images != null && Images.Count > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "offers", offer.Id.ToString());
            Directory.CreateDirectory(uploadFolder);

            int imageCount = 0;
            foreach (var image in Images)
            {
                if (imageCount >= 3) break;
                var extension = Path.GetExtension(image.FileName).ToLower();
                if (!allowedExtensions.Contains(extension)) continue;
                if (image.Length > 5 * 1024 * 1024) continue;
                var fileName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(uploadFolder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await image.CopyToAsync(stream);
                _context.RequestOfferImages.Add(new RequestOfferImage
                {
                    RequestOfferId = offer.Id,
                    FilePath = $"/uploads/offers/{offer.Id}/{fileName}"
                });
                imageCount++;
            }

            await _context.SaveChangesAsync();
        }

        var offerer = await _userManager.FindByIdAsync(userId);
        var offererName = offerer != null ? offerer.NamaDepan + " " + offerer.NamaBelakang : "Seseorang";

        _context.Notifications.Add(new Notification
        {
            UserId = itemRequest.UserId,
            Message = $"{offererName} menawarkan barang untuk request \"{itemRequest.Title}\" Anda.",
            Link = "/Request/MyRequests",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return RedirectToAction("Detail", new { id = itemRequestId });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptOffer(int offerId)
    {
        var userId = _userManager.GetUserId(User)!;

        var offer = await _context.RequestOffers
            .Include(o => o.ItemRequest)
            .FirstOrDefaultAsync(o => o.Id == offerId);

        if (offer == null || offer.ItemRequest == null || offer.ItemRequest.UserId != userId)
            return RedirectToAction("MyRequests", "Request");

        offer.Status = RequestOfferStatus.Accepted;
        offer.ItemRequest.Status = ItemRequestStatus.Fulfilled;

        _context.Notifications.Add(new Notification
        {
            UserId = offer.UserId,
            Message = $"Penawaran Anda untuk request \"{offer.ItemRequest.Title}\" telah diterima!",
            Link = "/Request/Detail/" + offer.ItemRequestId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return RedirectToAction("MyRequests", "Request");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectOffer(int offerId)
    {
        var userId = _userManager.GetUserId(User)!;

        var offer = await _context.RequestOffers
            .Include(o => o.ItemRequest)
            .FirstOrDefaultAsync(o => o.Id == offerId);

        if (offer == null || offer.ItemRequest == null || offer.ItemRequest.UserId != userId)
            return RedirectToAction("MyRequests", "Request");

        offer.Status = RequestOfferStatus.Rejected;

        _context.Notifications.Add(new Notification
        {
            UserId = offer.UserId,
            Message = $"Penawaran Anda untuk request \"{offer.ItemRequest.Title}\" ditolak.",
            Link = "/Request/Detail/" + offer.ItemRequestId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return RedirectToAction("MyRequests", "Request");
    }
}
