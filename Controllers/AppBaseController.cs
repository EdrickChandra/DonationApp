using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Controllers;

public class AppBaseController : Controller
{
    protected readonly AppDbContext _db;
    protected readonly UserManager<ApplicationUser> _userManager;

    public AppBaseController(AppDbContext db, UserManager<ApplicationUser> userManager)
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
                var userTask = _userManager.FindByIdAsync(userId);

                var notifTask = _db.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);

                var msgTask = _db.ChatMessages
                    .Where(m => m.SenderId != userId && !m.IsRead &&
                        _db.Conversations.Any(c => c.Id == m.ConversationId &&
                            (c.RequesterId == userId || c.DonorId == userId)))
                    .CountAsync();

                var ratingTask = _db.Feedbacks
                    .Where(f => f.ReviewedUserId == userId)
                    .AverageAsync(f => (double?)f.Rating);

                await Task.WhenAll(userTask, notifTask, msgTask, ratingTask);

                ViewBag.NavUser = userTask.Result;
                ViewBag.UnreadCount = notifTask.Result;
                ViewBag.UnreadMessageCount = msgTask.Result;
                ViewBag.AverageRating = Math.Round(ratingTask.Result ?? 0, 1);
            }
        }
        else
        {
            ViewBag.UnreadCount = 0;
            ViewBag.UnreadMessageCount = 0;
            ViewBag.AverageRating = 0.0;
        }

        await next();
    }
}
