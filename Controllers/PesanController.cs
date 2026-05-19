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
            .Where(c => c.RequesterId == userId || c.DonorId == userId)
            .ToListAsync();

        var conversationIds = conversations.Select(c => c.Id).ToList();

        var lastMessages = await _db.ChatMessages
            .Where(m => conversationIds.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => g.OrderByDescending(m => m.SentAt).First())
            .ToListAsync();

        foreach (var conv in conversations)
        {
            var lastMsg = lastMessages.FirstOrDefault(m => m.ConversationId == conv.Id);
            conv.Messages = lastMsg != null ? new List<ChatMessage> { lastMsg } : new List<ChatMessage>();
        }

        conversations = conversations
            .OrderByDescending(c =>
            {
                var lastMsg = lastMessages.FirstOrDefault(m => m.ConversationId == c.Id);
                return lastMsg?.SentAt ?? c.CreatedAt;
            })
            .ToList();

        ViewBag.Conversations = conversations;
        ViewBag.CurrentUserId = userId;
        ViewBag.InitialConvId = convId;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("~/Views/Profile/Pesan/Pesan.cshtml");

        ViewBag.InitialSection = "pesan";
        return View("~/Views/Profile/Shell.cshtml");
    }
}
