using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Controllers;

[Authorize]
public class ChatController : BaseController
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatController(AppDbContext context, UserManager<ApplicationUser> userManager)
        : base(context, userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetMessages(int conversationId)
    {
        var userId = _userManager.GetUserId(User)!;

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId &&
                (c.RequesterId == userId || c.DonorId == userId));

        if (conversation == null)
            return Forbid();

        var messages = await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.SentAt)
            .Select(m => new
            {
                m.Id,
                m.ConversationId,
                m.SenderId,
                m.Content,
                SentAt = m.SentAt.ToString("o"),
                m.IsRead
            })
            .ToListAsync();

        var unread = await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead)
            .ToListAsync();

        foreach (var msg in unread)
            msg.IsRead = true;

        await _context.SaveChangesAsync();

        return Json(messages);
    }

    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = _userManager.GetUserId(User)!;

        var count = await _context.ChatMessages
            .Where(m => m.SenderId != userId && !m.IsRead &&
                _context.Conversations.Any(c => c.Id == m.ConversationId &&
                    (c.RequesterId == userId || c.DonorId == userId)))
            .CountAsync();

        return Json(count);
    }
}