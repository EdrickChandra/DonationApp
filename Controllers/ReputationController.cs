using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Controllers;

[Authorize]
public class ReputationController : AppBaseController
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ReputationController(AppDbContext context, UserManager<ApplicationUser> userManager)
        : base(context, userManager)
    {
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int? claimRequestId, int? itemRequestId, int rating, string? komentar)
    {
        var userId = _userManager.GetUserId(User)!;

        if (rating < 1 || rating > 5)
            return RedirectToAction("Permintaan", "Request");

        if (claimRequestId == null && itemRequestId == null)
            return RedirectToAction("Permintaan", "Request");

        string reviewedUserId;

        if (claimRequestId.HasValue)
        {
            var claimRequest = await _db.ClaimRequests
                .Include(r => r.Item)
                .Include(r => r.Feedbacks)
                .FirstOrDefaultAsync(r => r.Id == claimRequestId.Value);

            if (claimRequest == null || claimRequest.Item == null)
                return RedirectToAction("Permintaan", "Request");
            if (claimRequest.UserId != userId)
                return RedirectToAction("Permintaan", "Request");
            if (claimRequest.Status != TransactionStatus.Delivered)
                return RedirectToAction("Permintaan", "Request");
            if (claimRequest.Feedbacks.Any(f => f.ReviewerId == userId))
                return RedirectToAction("Permintaan", "Request");
            if (claimRequest.Item.UserId == userId)
                return RedirectToAction("Permintaan", "Request");

            reviewedUserId = claimRequest.Item.UserId;
        }
        else
        {
            var itemRequest = await _db.ItemRequests
                .Include(r => r.Offers)
                .Include(r => r.Feedbacks)
                .FirstOrDefaultAsync(r => r.Id == itemRequestId!.Value);

            if (itemRequest == null)
                return RedirectToAction("MyRequests", "Request");
            if (itemRequest.UserId != userId)
                return RedirectToAction("MyRequests", "Request");
            if (itemRequest.Feedbacks.Any(f => f.ReviewerId == userId))
                return RedirectToAction("MyRequests", "Request");

            var acceptedOffer = itemRequest.Offers
                .FirstOrDefault(o => o.Status == TransactionStatus.Delivered);

            if (acceptedOffer == null)
                return RedirectToAction("MyRequests", "Request");
            if (acceptedOffer.UserId == userId)
                return RedirectToAction("MyRequests", "Request");

            reviewedUserId = acceptedOffer.UserId;
        }

        var feedback = new Feedback
        {
            ReviewerId = userId,
            ReviewedUserId = reviewedUserId,
            ClaimRequestId = claimRequestId,
            ItemRequestId = itemRequestId,
            Rating = rating,
            Komentar = string.IsNullOrWhiteSpace(komentar) ? null : komentar.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Feedbacks.Add(feedback);

        var existingRatings = await _db.Feedbacks
            .Where(f => f.ReviewedUserId == reviewedUserId)
            .Select(f => f.Rating)
            .ToListAsync();

        var newAverage = (existingRatings.Sum() + rating) / (double)(existingRatings.Count + 1);

        var reviewedUser = await _userManager.FindByIdAsync(reviewedUserId);
        if (reviewedUser != null)
        {
            reviewedUser.TrustScore = (decimal)Math.Round(newAverage, 1);
            await _userManager.UpdateAsync(reviewedUser);
        }

        _db.Notifications.Add(new Notification
        {
            UserId = reviewedUserId,
            Message = $"Anda mendapat rating {rating}/5.",
            Type = NotificationType.NewRating,
            Link = "/Profile/Overview",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return claimRequestId.HasValue
            ? RedirectToAction("Permintaan", "Request")
            : RedirectToAction("MyRequests", "Request");
    }
}