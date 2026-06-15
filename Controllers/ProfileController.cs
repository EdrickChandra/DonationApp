using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Controllers;

[Authorize]
public class ProfileController : AppBaseController
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(AppDbContext db, UserManager<ApplicationUser> userManager)
        : base(db, userManager)
    {
        _userManager = userManager;
    }

    public IActionResult Index(string? section)
    {
        ViewBag.InitialSection = section ?? "overview";
        return base.View("~/Views/Profile/Shell.cshtml");
    }

    public async Task<IActionResult> Overview()
    {
        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.FindByIdAsync(userId);

        var totalDonasiTask = _db.Items.CountAsync(i => i.UserId == userId);
        var activeDonasiTask = _db.Items.CountAsync(i => i.UserId == userId && i.Status == ItemStatus.Available && i.ExpiresAt > DateTime.UtcNow);
        var totalRequestTask = _db.ItemRequests.CountAsync(r => r.UserId == userId);
        var openRequestTask = _db.ItemRequests.CountAsync(r => r.UserId == userId && r.Status == ItemRequestStatus.Open);
        var totalReviewsTask = _db.Feedbacks.CountAsync(f => f.ReviewedUserId == userId);
        var completedDonasiTask = _db.ClaimRequests
            .CountAsync(r => r.Item!.UserId == userId && r.Status == TransactionStatus.Delivered);
        var completedOfferTask = _db.RequestOffers
            .CountAsync(o => o.UserId == userId && o.Status == TransactionStatus.Delivered);
        var peoplehelpedTask = _db.ClaimRequests
            .Where(r => r.Item!.UserId == userId && r.Status == TransactionStatus.Delivered)
            .Select(r => r.UserId).Distinct().CountAsync();

        await Task.WhenAll(totalDonasiTask, activeDonasiTask, totalRequestTask, openRequestTask,
            totalReviewsTask, completedDonasiTask, completedOfferTask, peoplehelpedTask);

        var recentReviews = await _db.Feedbacks
            .Include(f => f.Reviewer)
            .Include(f => f.ClaimRequest)
                .ThenInclude(c => c!.Item)
            .Include(f => f.ItemRequest)
            .Where(f => f.ReviewedUserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Take(5)
            .ToListAsync();

        var activeDonations = await _db.Items
            .Include(i => i.Images)
            .Where(i => i.UserId == userId && i.Status == ItemStatus.Available && i.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(i => i.CreatedAt)
            .Take(4)
            .ToListAsync();

        var activeRequests = await _db.ItemRequests
            .Include(r => r.Images)
            .Where(r => r.UserId == userId && r.Status == ItemRequestStatus.Open && r.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(r => r.CreatedAt)
            .Take(4)
            .ToListAsync();

        ViewBag.TotalDonasi = totalDonasiTask.Result;
        ViewBag.ActiveDonasi = activeDonasiTask.Result;
        ViewBag.TotalRequest = totalRequestTask.Result;
        ViewBag.OpenRequest = openRequestTask.Result;
        ViewBag.AvgRating = (double)(user?.AvgRating ?? 0);
        ViewBag.TotalReviews = totalReviewsTask.Result;
        ViewBag.RecentReviews = recentReviews;
        ViewBag.CompletedDonasi = completedDonasiTask.Result + completedOfferTask.Result;
        ViewBag.PeopleHelped = peoplehelpedTask.Result;
        ViewBag.TotalPoin = user?.TotalPoin ?? 0;
        ViewBag.ActiveDonations = activeDonations;
        ViewBag.ActiveRequests = activeRequests;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("~/Views/Profile/Overview.cshtml");

        ViewBag.InitialSection = "overview";
        return base.View("~/Views/Profile/Shell.cshtml");
    }

    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("~/Views/Profile/Edit.cshtml", user);

        ViewBag.InitialSection = "profil";
        return base.View("~/Views/Profile/Shell.cshtml");
    }

    [AllowAnonymous]
    public async Task<IActionResult> View(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || user.IsBanned) return NotFound();

        var totalDonasi = await _db.Items.CountAsync(i => i.UserId == id);
        var totalRequest = await _db.ItemRequests.CountAsync(r => r.UserId == id);
        var totalReviews = await _db.Feedbacks.CountAsync(f => f.ReviewedUserId == id);

        var reviews = await _db.Feedbacks
            .Include(f => f.Reviewer)
            .Include(f => f.ClaimRequest)
                .ThenInclude(c => c!.Item)
            .Include(f => f.ItemRequest)
            .Where(f => f.ReviewedUserId == id)
            .OrderByDescending(f => f.CreatedAt)
            .Take(20)
            .ToListAsync();

        var activeDonations = await _db.Items
            .Include(i => i.Images)
            .Where(i => i.UserId == id && i.Status == ItemStatus.Available && i.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(i => i.CreatedAt)
            .Take(6)
            .ToListAsync();

        var activeRequests = await _db.ItemRequests
            .Include(r => r.Images)
            .Where(r => r.UserId == id && r.Status == ItemRequestStatus.Open && r.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(r => r.CreatedAt)
            .Take(6)
            .ToListAsync();

        ViewBag.ProfileUser = user;
        ViewBag.TotalDonasi = totalDonasi;
        ViewBag.TotalRequest = totalRequest;
        ViewBag.AvgRating = (double)user.AvgRating;
        ViewBag.TotalReviews = totalReviews;
        ViewBag.Reviews = reviews;
        ViewBag.ActiveDonations = activeDonations;
        ViewBag.ActiveRequests = activeRequests;

        return base.View("~/Views/Profile/PublicProfile.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string NamaDepan, string NamaBelakang, string PhoneNumber, string Alamat, string Kota, string Provinsi, string KodePos)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        user.NamaDepan = NamaDepan;
        user.NamaBelakang = NamaBelakang;
        user.PhoneNumber = PhoneNumber;
        user.Alamat = Alamat;
        user.Kota = Kota;
        user.Provinsi = Provinsi;
        user.KodePos = KodePos;
        user.UserName = NamaDepan;

        await _userManager.UpdateAsync(user);
        TempData["EditSuccess"] = "Profil berhasil diperbarui.";
        return RedirectToAction("Edit");
    }

    public async Task<IActionResult> Notifikasi(string tab = "unread")
    {
        var userId = _userManager.GetUserId(User)!;

        var unreadTask = _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        var readTask = _db.Notifications
            .Where(n => n.UserId == userId && n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(30)
            .ToListAsync();

        await Task.WhenAll(unreadTask, readTask);

        ViewBag.Tab = tab;
        ViewBag.Unread = unreadTask.Result;
        ViewBag.Read = readTask.Result;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("~/Views/Profile/Notifikasi/Notifikasi.cshtml");

        ViewBag.InitialSection = "notifikasi";
        return base.View("~/Views/Profile/Shell.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var notif = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notif != null)
        {
            notif.IsRead = true;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Notifikasi", new { tab = "unread" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = _userManager.GetUserId(User)!;
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync();
        return RedirectToAction("Notifikasi", new { tab = "read" });
    }
}
