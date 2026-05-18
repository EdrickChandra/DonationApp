using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Controllers;

[Authorize]
public class ProfileController : ProfileBaseController
{
    private readonly UserManager<ApplicationUser> _um;

    public ProfileController(AppDbContext db, UserManager<ApplicationUser> userManager)
        : base(db, userManager)
    {
        _um = userManager;
    }

    public async Task<IActionResult> Overview()
    {
        var userId = _um.GetUserId(User)!;

        var totalDonasi = await _db.Items.CountAsync(i => i.UserId == userId);
        var activeDonasi = await _db.Items.CountAsync(i => i.UserId == userId && i.Status == ItemStatus.Available && i.ExpiresAt > DateTime.UtcNow);

        var totalRequest = await _db.ItemRequests.CountAsync(r => r.UserId == userId);
        var openRequest = await _db.ItemRequests.CountAsync(r => r.UserId == userId && r.Status == ItemRequestStatus.Open);

        var avgRating = await _db.UserReputations
            .Where(r => r.ReviewedUserId == userId)
            .AverageAsync(r => (double?)r.Rating) ?? 0;

        var totalReviews = await _db.UserReputations.CountAsync(r => r.ReviewedUserId == userId);

        var recentReviews = await _db.UserReputations
            .Include(r => r.Reviewer)
            .Include(r => r.ClaimRequest)
                .ThenInclude(c => c!.Item)
            .Where(r => r.ReviewedUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.TotalDonasi = totalDonasi;
        ViewBag.ActiveDonasi = activeDonasi;
        ViewBag.TotalRequest = totalRequest;
        ViewBag.OpenRequest = openRequest;
        ViewBag.AvgRating = Math.Round(avgRating, 1);
        ViewBag.TotalReviews = totalReviews;
        ViewBag.RecentReviews = recentReviews;

        return View();
    }

    public async Task<IActionResult> Edit()
    {
        var user = await _um.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string NamaDepan, string NamaBelakang, string NomorTelepon, string Alamat, string Provinsi, string KodePos)
    {
        var user = await _um.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        user.NamaDepan = NamaDepan;
        user.NamaBelakang = NamaBelakang;
        user.NomorTelepon = NomorTelepon;
        user.Alamat = Alamat;
        user.Provinsi = Provinsi;
        user.KodePos = KodePos;
        user.UserName = NamaDepan;

        await _um.UpdateAsync(user);
        TempData["EditSuccess"] = "Profil berhasil diperbarui.";
        return RedirectToAction("Edit");
    }
}