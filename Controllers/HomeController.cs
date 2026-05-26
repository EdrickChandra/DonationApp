using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;
using Microsoft.AspNetCore.Identity;

namespace DonationApp.Controllers;

public class HomeController : AppBaseController
{
    public HomeController(AppDbContext context, UserManager<ApplicationUser> userManager)
        : base(context, userManager)
    {
    }

    public async Task<IActionResult> Index(ItemCategory? kategori)
    {
        var query = _db.Items
            .Include(i => i.Images)
            .Where(i => i.Status == ItemStatus.Available && i.ExpiresAt > DateTime.UtcNow);

        if (kategori.HasValue)
            query = query.Where(i => i.Kategori == kategori.Value);

        var items = await query.OrderByDescending(i => i.CreatedAt).ToListAsync();

        ViewBag.SelectedKategori = kategori;

        return View(items);
    }
}
