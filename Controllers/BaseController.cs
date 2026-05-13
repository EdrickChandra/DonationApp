using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Controllers;

public class BaseController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public BaseController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User);
            if (userId != null)
            {
                var unreadNotifCount = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);
                ViewBag.UnreadCount = unreadNotifCount;

                var unreadMessageCount = await _context.ChatMessages
                    .Where(m => m.SenderId != userId && !m.IsRead &&
                        _context.Conversations.Any(c => c.Id == m.ConversationId &&
                            (c.RequesterId == userId || c.DonorId == userId)))
                    .CountAsync();
                ViewBag.UnreadMessageCount = unreadMessageCount;
            }
        }
        else
        {
            ViewBag.UnreadCount = 0;
            ViewBag.UnreadMessageCount = 0;
        }

        await next();
    }
}