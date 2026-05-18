using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Controllers;

public class ProfileBaseController : Controller
{
    protected readonly AppDbContext _db;
    protected readonly UserManager<ApplicationUser> _userManager;

    public ProfileBaseController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User);
            if (userId != null)
            {
                var user = await _userManager.FindByIdAsync(userId);
                ViewBag.NavUser = user;

                ViewBag.UnreadCount = await _db.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                ViewBag.UnreadMessageCount = await _db.ChatMessages
                    .Where(m => m.SenderId != userId && !m.IsRead &&
                        _db.Conversations.Any(c => c.Id == m.ConversationId &&
                            (c.RequesterId == userId || c.DonorId == userId)))
                    .CountAsync();

                var avgRating = await _db.UserReputations
                    .Where(r => r.ReviewedUserId == userId)
                    .AverageAsync(r => (double?)r.Rating) ?? 0;

                ViewBag.AverageRating = Math.Round(avgRating, 1);
            }
        }

        await next();
    }
}