using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Controllers;

[Authorize]
public class NotifikasiController : ProfileBaseController
{
    private readonly UserManager<ApplicationUser> _um;

    public NotifikasiController(AppDbContext db, UserManager<ApplicationUser> userManager)
        : base(db, userManager)
    {
        _um = userManager;
    }

    public async Task<IActionResult> Index(string tab = "unread")
    {
        var userId = _um.GetUserId(User)!;

        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        var read = await _db.Notifications
            .Where(n => n.UserId == userId && n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(30)
            .ToListAsync();

        ViewBag.Tab = tab;
        ViewBag.Unread = unread;
        ViewBag.Read = read;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("~/Views/Profile/Notifikasi/Notifikasi.cshtml");

        ViewBag.InitialSection = "notifikasi";
        return View("~/Views/Profile/Shell.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = _um.GetUserId(User)!;
        var notif = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notif != null) { notif.IsRead = true; await _db.SaveChangesAsync(); }
        return RedirectToAction("Index", new { tab = "unread" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = _um.GetUserId(User)!;
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync();
        return RedirectToAction("Index", new { tab = "read" });
    }
}
