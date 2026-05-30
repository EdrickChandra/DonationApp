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
    public async Task<IActionResult> Submit(int? claimRequestId, int? itemRequestId, int? requestOfferId, int rating, string? komentar)
    {
        var userId = _userManager.GetUserId(User)!;

        if (rating < 1 || rating > 5)
            return RedirectToAction("Permintaan", "Request");

        if (claimRequestId == null && itemRequestId == null && requestOfferId == null)
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
            if (claimRequest.Status != TransactionStatus.Delivered)
                return RedirectToAction("Permintaan", "Request");
            if (claimRequest.Feedbacks.Any(f => f.ReviewerId == userId))
                return RedirectToAction("Permintaan", "Request");

            if (claimRequest.UserId == userId)
            {
                reviewedUserId = claimRequest.Item.UserId;
            }
            else if (claimRequest.Item.UserId == userId)
            {
                reviewedUserId = claimRequest.UserId;
            }
            else
            {
                return RedirectToAction("Permintaan", "Request");
            }
        }
        else if (requestOfferId.HasValue)
        {
            var offer = await _db.RequestOffers
                .Include(o => o.ItemRequest)
                .FirstOrDefaultAsync(o => o.Id == requestOfferId.Value);

            if (offer == null || offer.ItemRequest == null)
                return RedirectToAction("MyRequests", "Request");
            if (offer.Status != TransactionStatus.Delivered)
                return RedirectToAction("MyRequests", "Request");

            var existingFeedback = await _db.Feedbacks
                .AnyAsync(f => f.ReviewerId == userId && f.ItemRequestId == offer.ItemRequestId
                    && ((f.ReviewedUserId == offer.UserId) || (f.ReviewedUserId == offer.ItemRequest.UserId)));

            if (existingFeedback)
                return RedirectToAction("MyRequests", "Request");

            if (offer.ItemRequest.UserId == userId)
            {
                reviewedUserId = offer.UserId;
            }
            else if (offer.UserId == userId)
            {
                reviewedUserId = offer.ItemRequest.UserId;
            }
            else
            {
                return RedirectToAction("MyRequests", "Request");
            }

            itemRequestId = offer.ItemRequestId;
        }
        else
        {
            var itemRequest = await _db.ItemRequests
                .Include(r => r.Offers)
                .Include(r => r.Feedbacks)
                .FirstOrDefaultAsync(r => r.Id == itemRequestId!.Value);

            if (itemRequest == null)
                return RedirectToAction("MyRequests", "Request");

            var acceptedOffer = itemRequest.Offers
                .FirstOrDefault(o => o.Status == TransactionStatus.Delivered);

            if (acceptedOffer == null)
                return RedirectToAction("MyRequests", "Request");

            if (itemRequest.UserId == userId)
            {
                if (itemRequest.Feedbacks.Any(f => f.ReviewerId == userId))
                    return RedirectToAction("MyRequests", "Request");
                reviewedUserId = acceptedOffer.UserId;
            }
            else if (acceptedOffer.UserId == userId)
            {
                if (itemRequest.Feedbacks.Any(f => f.ReviewerId == userId))
                    return RedirectToAction("MyRequests", "Request");
                reviewedUserId = itemRequest.UserId;
            }
            else
            {
                return RedirectToAction("MyRequests", "Request");
            }
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

        if (claimRequestId.HasValue)
            return RedirectToAction("Permintaan", "Request");
        return RedirectToAction("MyRequests", "Request");
    }
}