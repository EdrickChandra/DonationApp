using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;
using Microsoft.AspNetCore.Identity;

namespace DonationApp.Controllers;

public class SearchController : BaseController
{
    private readonly AppDbContext _context;

    public SearchController(AppDbContext context, UserManager<ApplicationUser> userManager)
        : base(context, userManager)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? q, string? tab, ItemCategory? kategori)
    {
        var query = q?.Trim() ?? string.Empty;
        var activeTab = tab ?? "donasi";

        var donationsQuery = _context.Items
            .Include(i => i.Images)
            .Include(i => i.User)
            .Where(i => i.Status == ItemStatus.Available && i.ExpiresAt > DateTime.UtcNow);

        var requestsQuery = _context.ItemRequests
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

        if (kategori.HasValue && kategori.Value != ItemCategory.Semua)
        {
            donationsQuery = donationsQuery.Where(i => i.Kategori == kategori.Value);
            requestsQuery = requestsQuery.Where(r => r.Kategori == kategori.Value);
        }

        var donations = await donationsQuery.OrderByDescending(i => i.CreatedAt).ToListAsync();
        var requests = await requestsQuery.OrderByDescending(r => r.CreatedAt).ToListAsync();

        ViewBag.Query = query;
        ViewBag.ActiveTab = activeTab;
        ViewBag.SelectedKategori = kategori ?? ItemCategory.Semua;
        ViewBag.Donations = donations;
        ViewBag.Requests = requests;

        return View("Search");
    }
}