using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Controllers;

[Authorize]
public class ProfileController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;

    public ProfileController(UserManager<ApplicationUser> userManager, AppDbContext context)
        : base(context, userManager)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index(string section = "overview")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var userId = user.Id;

        var donations = await _context.Items
            .Include(i => i.Images)
            .Include(i => i.ClaimRequests)
                .ThenInclude(r => r.User)
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        var requests = await _context.ClaimRequests
            .Include(r => r.Item)
                .ThenInclude(i => i!.Images)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        foreach (var item in donations)
        {
            if (item.Status == ItemStatus.Available && item.ExpiresAt <= DateTime.UtcNow)
            {
                var alreadyNotified = await _context.Notifications
                    .AnyAsync(n => n.UserId == userId && n.Link == "/Profile?section=donasi" && n.Message.Contains(item.NamaBarang) && n.Message.Contains("kedaluwarsa"));

                if (!alreadyNotified)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = userId,
                        Message = $"Barang \"{item.NamaBarang}\" Anda telah kedaluwarsa dan tidak lagi ditampilkan.",
                        Link = "/Profile?section=donasi",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _context.SaveChangesAsync();

        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        ViewBag.Section = section;
        ViewBag.User = user;
        ViewBag.Donations = donations;
        ViewBag.Requests = requests;
        ViewBag.Notifications = notifications;

        return View("Profile");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string NamaDepan, string NamaBelakang, string NomorTelepon, string Alamat, string Provinsi, string KodePos)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        user.NamaDepan = NamaDepan;
        user.NamaBelakang = NamaBelakang;
        user.NomorTelepon = NomorTelepon;
        user.Alamat = Alamat;
        user.Provinsi = Provinsi;
        user.KodePos = KodePos;
        user.UserName = NamaDepan;

        await _userManager.UpdateAsync(user);
        return RedirectToAction("Index", new { section = "profil" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int notificationId)
    {
        var userId = _userManager.GetUserId(User)!;

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Index", new { section = "notifikasi" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = _userManager.GetUserId(User)!;

        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
            n.IsRead = true;

        await _context.SaveChangesAsync();
        return RedirectToAction("Index", new { section = "notifikasi" });
    }
}