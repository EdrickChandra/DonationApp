using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Controllers;

[Authorize]
public class PesanController : ProfileBaseController
{
    private readonly UserManager<ApplicationUser> _um;

    public PesanController(AppDbContext db, UserManager<ApplicationUser> userManager)
        : base(db, userManager)
    {
        _um = userManager;
    }

    public async Task<IActionResult> Index(int? convId)
    {
        var userId = _um.GetUserId(User)!;

        var conversations = await _db.Conversations
            .Include(c => c.Item)
                .ThenInclude(i => i!.Images)
            .Include(c => c.Requester)
            .Include(c => c.Donor)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
            .Where(c => c.RequesterId == userId || c.DonorId == userId)
            .OrderByDescending(c => c.Messages.Max(m => (DateTime?)m.SentAt) ?? c.CreatedAt)
            .ToListAsync();

        ViewBag.Conversations = conversations;
        ViewBag.CurrentUserId = userId;
        ViewBag.InitialConvId = convId;

        return View("~/Views/Profile/Pesan/Pesan.cshtml");
    }
}