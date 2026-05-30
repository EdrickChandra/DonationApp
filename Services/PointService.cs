using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Services;

public class PointsService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public const int PointsPerDonation = 10;

    public PointsService(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task AwardPointsAsync(string userId, int? claimRequestId, int? requestOfferId)
    {
        var already = await _db.PointTransactions
            .AnyAsync(p => p.UserId == userId
                && p.Type == PointTransactionType.DonationCompleted
                && (claimRequestId != null ? p.ClaimRequestId == claimRequestId : p.RequestOfferId == requestOfferId));

        if (already) return;

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        _db.PointTransactions.Add(new PointTransaction
        {
            UserId = userId,
            Type = PointTransactionType.DonationCompleted,
            Amount = PointsPerDonation,
            ClaimRequestId = claimRequestId,
            RequestOfferId = requestOfferId,
            CreatedAt = DateTime.UtcNow
        });

        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Message = $"Selamat! Anda mendapatkan {PointsPerDonation} poin atas kontribusi donasi Anda.",
            Type = NotificationType.PointEarned,
            Link = "/Redeem/Index",
            CreatedAt = DateTime.UtcNow
        });

        user.TotalPoin += PointsPerDonation;
        await _db.SaveChangesAsync();
    }

    public async Task<string?> RedeemAsync(string userId, int redeemItemId)
    {
        var item = await _db.RedeemItems.FindAsync(redeemItemId);
        if (item == null || !item.IsActive)
            return "Item tidak ditemukan.";

        if (item.Stock <= 0)
            return "Stok habis.";

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return "Pengguna tidak ditemukan.";

        if (user.TotalPoin < item.PointCost)
            return "Poin tidak cukup.";

        user.TotalPoin -= item.PointCost;
        await _userManager.UpdateAsync(user);

        item.Stock -= 1;

        _db.PointTransactions.Add(new PointTransaction
        {
            UserId = userId,
            Type = PointTransactionType.SpendPoint,
            Amount = -item.PointCost,
            RedeemItemId = redeemItemId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return null;
    }
}