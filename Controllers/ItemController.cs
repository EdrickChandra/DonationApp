using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Controllers;

public class ItemController : Controller
{
    private readonly AppDbContext _context;

    public ItemController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> MyDonations()
    {
        var items = await _context.Items
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return View(items);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Item item)
    {
        if (!ModelState.IsValid)
            return View(item);

        item.CreatedAt = DateTime.UtcNow;
        item.ExpiresAt = DateTime.UtcNow.AddDays(7);
        item.Status = ItemStatus.Available;

        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(MyDonations));
    }
}
