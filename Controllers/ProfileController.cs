using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;

    public ProfileController(UserManager<ApplicationUser> userManager, AppDbContext context)
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
                .ThenInclude(i => i.Images)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        ViewBag.Section = section;
        ViewBag.User = user;
        ViewBag.Donations = donations;
        ViewBag.Requests = requests;

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
}