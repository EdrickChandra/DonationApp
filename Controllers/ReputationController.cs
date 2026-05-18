using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Controllers;

[Authorize]
public class ReputationController : BaseController
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReputationController(AppDbContext context, UserManager<ApplicationUser> userManager)
        : base(context, userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int claimRequestId, int rating, string? komentar)
    {
        var userId = _userManager.GetUserId(User)!;

        var claimRequest = await _context.ClaimRequests
            .Include(r => r.Item)
            .Include(r => r.Reputations)
            .FirstOrDefaultAsync(r => r.Id == claimRequestId);

        if (claimRequest == null || claimRequest.Item == null)
            return RedirectToAction("Permintaan", "Request");

        if (claimRequest.UserId != userId)
            return RedirectToAction("Permintaan", "Request");

        if (claimRequest.Status != ClaimRequestStatus.Delivered)
            return RedirectToAction("Permintaan", "Request");

        var alreadyReviewed = claimRequest.Reputations.Any(r => r.ReviewerId == userId);
        if (alreadyReviewed)
            return RedirectToAction("Permintaan", "Request");

        if (rating < 1 || rating > 5)
            return RedirectToAction("Permintaan", "Request");

        var reputation = new UserReputation
        {
            ReviewerId = userId,
            ReviewedUserId = claimRequest.Item.UserId,
            ClaimRequestId = claimRequestId,
            Rating = rating,
            Komentar = string.IsNullOrWhiteSpace(komentar) ? null : komentar.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.UserReputations.Add(reputation);

        var donorReputations = await _context.UserReputations
            .Where(r => r.ReviewedUserId == claimRequest.Item.UserId)
            .ToListAsync();

        var newAverage = (donorReputations.Sum(r => r.Rating) + rating) / (double)(donorReputations.Count + 1);

        var donor = await _userManager.FindByIdAsync(claimRequest.Item.UserId);
        if (donor != null)
        {
            donor.TrustScore = (decimal)Math.Round(newAverage, 1);
            await _userManager.UpdateAsync(donor);
        }

        _context.Notifications.Add(new Notification
        {
            UserId = claimRequest.Item.UserId,
            Message = $"Anda mendapat rating {rating}/5 untuk donasi \"{claimRequest.Item.NamaBarang}\".",
            Link = "/Profile/Edit",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return RedirectToAction("Permintaan", "Request");
    }
}