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
        return View("~/Views/Profile/Shell.cshtml");
    }

    public async Task<IActionResult> Overview()
    {
        var userId = _userManager.GetUserId(User)!;

        var totalDonasiTask = _db.Items.CountAsync(i => i.UserId == userId);
        var activeDonasiTask = _db.Items.CountAsync(i => i.UserId == userId && i.Status == ItemStatus.Available && i.ExpiresAt > DateTime.UtcNow);
        var totalRequestTask = _db.ItemRequests.CountAsync(r => r.UserId == userId);
        var openRequestTask = _db.ItemRequests.CountAsync(r => r.UserId == userId && r.Status == ItemRequestStatus.Open);
        var avgRatingTask = _db.Feedbacks.Where(f => f.ReviewedUserId == userId).AverageAsync(f => (double?)f.Rating);
        var totalReviewsTask = _db.Feedbacks.CountAsync(f => f.ReviewedUserId == userId);

        await Task.WhenAll(totalDonasiTask, activeDonasiTask, totalRequestTask, openRequestTask, avgRatingTask, totalReviewsTask);

        var recentReviews = await _db.Feedbacks
            .Include(f => f.Reviewer)
            .Include(f => f.ClaimRequest)
                .ThenInclude(c => c!.Item)
            .Where(f => f.ReviewedUserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.TotalDonasi = totalDonasiTask.Result;
        ViewBag.ActiveDonasi = activeDonasiTask.Result;
        ViewBag.TotalRequest = totalRequestTask.Result;
        ViewBag.OpenRequest = openRequestTask.Result;
        ViewBag.AvgRating = Math.Round(avgRatingTask.Result ?? 0, 1);
        ViewBag.TotalReviews = totalReviewsTask.Result;
        ViewBag.RecentReviews = recentReviews;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("~/Views/Profile/Overview.cshtml");

        ViewBag.InitialSection = "overview";
        return View("~/Views/Profile/Shell.cshtml");
    }

    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("~/Views/Profile/Edit.cshtml", user);

        ViewBag.InitialSection = "profil";
        return View("~/Views/Profile/Shell.cshtml");
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
        return View("~/Views/Profile/Shell.cshtml");
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
