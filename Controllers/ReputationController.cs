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
    public async Task<IActionResult> Submit(int? claimRequestId, int? requestOfferId, int rating, string? komentar)
    {
        var userId = _userManager.GetUserId(User)!;

        if (rating < 1 || rating > 5)
            return RedirectToAction("Permintaan", "Request");

        // Must supply exactly one source
        if (claimRequestId == null && requestOfferId == null)
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
            if (claimRequest.Status != ClaimRequestStatus.Delivered)
                return RedirectToAction("Permintaan", "Request");
            if (claimRequest.Feedbacks.Any(f => f.ReviewerId == userId))
                return RedirectToAction("Permintaan", "Request");
            if (claimRequest.Item.UserId == userId)
                return RedirectToAction("Permintaan", "Request");

            reviewedUserId = claimRequest.Item.UserId;
        }
        else
        {
            var offer = await _db.RequestOffers
                .Include(o => o.ItemRequest)
                .Include(o => o.Feedbacks)
                .FirstOrDefaultAsync(o => o.Id == requestOfferId!.Value);

            if (offer == null || offer.ItemRequest == null)
                return RedirectToAction("MyRequests", "Request");
            if (offer.ItemRequest.UserId != userId)
                return RedirectToAction("MyRequests", "Request");
            if (offer.Status != RequestOfferStatus.Accepted)
                return RedirectToAction("MyRequests", "Request");
            if (offer.Feedbacks.Any(f => f.ReviewerId == userId))
                return RedirectToAction("MyRequests", "Request");
            if (offer.UserId == userId)
                return RedirectToAction("MyRequests", "Request");

            reviewedUserId = offer.UserId;
        }

        var feedback = new Feedback
        {
            ReviewerId = userId,
            ReviewedUserId = reviewedUserId,
            ClaimRequestId = claimRequestId,
            RequestOfferId = requestOfferId,
            Rating = rating,
            Komentar = string.IsNullOrWhiteSpace(komentar) ? null : komentar.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Feedbacks.Add(feedback);

        // Update donor TrustScore
        var existingRatings = await _db.Feedbacks
            .Where(f => f.ReviewedUserId == reviewedUserId)
            .Select(f => f.Rating)
            .ToListAsync();

        var newAverage = (existingRatings.Sum() + rating) / (double)(existingRatings.Count + 1);

        var donor = await _userManager.FindByIdAsync(reviewedUserId);
        if (donor != null)
        {
            donor.TrustScore = (decimal)Math.Round(newAverage, 1);
            await _userManager.UpdateAsync(donor);
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
