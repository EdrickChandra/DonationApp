using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;
using Microsoft.AspNetCore.Identity;

namespace DonationApp.Controllers;

public class SearchController : AppBaseController
{
    public SearchController(AppDbContext context, UserManager<ApplicationUser> userManager)
        : base(context, userManager)
    {
    }

    public async Task<IActionResult> Index(string? q, string? tab, ItemCategory? kategori)
    {
        var query = q?.Trim() ?? string.Empty;
        var activeTab = tab ?? "donasi";

        var donationsQuery = _db.Items
            .Include(i => i.Images)
            .Include(i => i.User)
            .Where(i => i.Status == ItemStatus.Available && i.ExpiresAt > DateTime.UtcNow);

        var requestsQuery = _db.ItemRequests
            .Include(r => r.Images)
            .Include(r => r.User)
            .Include(r => r.Offers)
            .Where(r => r.Status == ItemRequestStatus.Open && r.ExpiresAt > DateTime.UtcNow);

        if (!string.IsNullOrWhiteSpace(query))
        {
            donationsQuery = donationsQuery.Where(i =>
                i.NamaBarang.Contains(query) ||
                i.Deskripsi.Contains(query) ||
                i.Lokasi.Contains(query));

            requestsQuery = requestsQuery.Where(r =>
                r.Title.Contains(query) ||
                r.Deskripsi.Contains(query) ||
                r.Lokasi.Contains(query));
        }

        if (kategori.HasValue)
        {
            donationsQuery = donationsQuery.Where(i => i.Kategori == kategori.Value);
            requestsQuery = requestsQuery.Where(r => r.Kategori == kategori.Value);
        }

        var donationsTask = donationsQuery.OrderByDescending(i => i.CreatedAt).Take(50).ToListAsync();
        var requestsTask = requestsQuery.OrderByDescending(r => r.CreatedAt).Take(50).ToListAsync();

        await Task.WhenAll(donationsTask, requestsTask);

        ViewBag.Query = query;
        ViewBag.ActiveTab = activeTab;
        ViewBag.SelectedKategori = kategori;
        ViewBag.Donations = donationsTask.Result;
        ViewBag.Requests = requestsTask.Result;

        return View("Search");
    }
}
