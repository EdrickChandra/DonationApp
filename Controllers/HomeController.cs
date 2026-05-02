using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(ItemCategory? kategori)
    {
        var query = _context.Items
            .Where(i => i.Status == ItemStatus.Available && i.ExpiresAt > DateTime.UtcNow);

        if (kategori.HasValue && kategori.Value != ItemCategory.Semua)
            query = query.Where(i => i.Kategori == kategori.Value);

        var items = await query.OrderByDescending(i => i.CreatedAt).ToListAsync();

        ViewBag.SelectedKategori = kategori ?? ItemCategory.Semua;

        return View(items);
    }
}
