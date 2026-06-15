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
    private readonly IWebHostEnvironment _env;

    public ReputationController(AppDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        : base(context, userManager)
    {
        _userManager = userManager;
        _env = env;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int? claimRequestId, int? itemRequestId, int? requestOfferId, int rating, string? komentar, IFormFile? foto)
    {
        var userId = _userManager.GetUserId(User)!;

        if (rating < 1 || rating > 5)
        {
            TempData["RequestError"] = "Silakan pilih rating terlebih dahulu.";
            if (claimRequestId.HasValue)
                return RedirectToAction("Permintaan", "Request");
            return RedirectToAction("MyRequests", "Request");
        }

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

            if (claimRequest.UserId != userId)
                return RedirectToAction("Permintaan", "Request");

            reviewedUserId = claimRequest.Item.UserId;
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
                .AnyAsync(f => f.ReviewerId == userId && f.ItemRequestId == offer.ItemRequestId);

            if (existingFeedback)
                return RedirectToAction("MyRequests", "Request");

            if (offer.ItemRequest.UserId != userId)
                return RedirectToAction("MyRequests", "Request");

            reviewedUserId = offer.UserId;

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

            if (itemRequest.UserId != userId)
                return RedirectToAction("MyRequests", "Request");

            if (itemRequest.Feedbacks.Any(f => f.ReviewerId == userId))
                return RedirectToAction("MyRequests", "Request");

            reviewedUserId = acceptedOffer.UserId;
        }

        string? fotoPath = null;
        if (foto != null && foto.Length > 0)
        {
            var fileName = Guid.NewGuid() + Path.GetExtension(foto.FileName);
            var relativePath = $"/uploads/feedback/{userId}/{fileName}";
            var fullPath = Path.Combine(_env.WebRootPath, "uploads", "feedback", userId, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            using var stream = new FileStream(fullPath, FileMode.Create);
            await foto.CopyToAsync(stream);
            fotoPath = relativePath;
        }

        var feedback = new Feedback
        {
            ReviewerId = userId,
            ReviewedUserId = reviewedUserId,
            ClaimRequestId = claimRequestId,
            ItemRequestId = itemRequestId,
            Rating = rating,
            Komentar = string.IsNullOrWhiteSpace(komentar) ? null : komentar.Trim(),
            FotoPath = fotoPath,
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
            reviewedUser.AvgRating = (decimal)Math.Round(newAverage, 1);
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

        TempData["RequestSuccess"] = "Feedback berhasil dikirim!";
        if (claimRequestId.HasValue)
            return RedirectToAction("Permintaan", "Request", new { selectedId = claimRequestId.Value });
        if (requestOfferId.HasValue)
        {
            var offr = await _db.RequestOffers.FindAsync(requestOfferId.Value);
            if (offr != null)
                return RedirectToAction("MyRequests", "Request", new { selectedId = offr.ItemRequestId });
        }
        return RedirectToAction("MyRequests", "Request");
    }
}