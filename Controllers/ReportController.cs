using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Controllers;

[Authorize]
public class ReportController : AppBaseController
{
    public ReportController(AppDbContext db, UserManager<ApplicationUser> userManager)
        : base(db, userManager)
    {
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int? donationId, int? requestId, ReportReason alasan, string? deskripsi)
    {
        var userId = _userManager.GetUserId(User)!;

        if (donationId == null && requestId == null)
            return Json(new { success = false, error = "Target laporan tidak valid." });

        string? targetUserId = null;
        if (donationId.HasValue)
        {
            var item = await _db.Items.FindAsync(donationId.Value);
            if (item == null) return Json(new { success = false, error = "Item tidak ditemukan." });
            targetUserId = item.UserId;
        }
        else if (requestId.HasValue)
        {
            var req = await _db.ItemRequests.FindAsync(requestId.Value);
            if (req == null) return Json(new { success = false, error = "Request tidak ditemukan." });
            targetUserId = req.UserId;
        }

        var report = new Report
        {
            ReporterId = userId,
            TargetUserId = targetUserId,
            TargetDonationId = donationId,
            TargetRequestId = requestId,
            Alasan = alasan,
            Deskripsi = string.IsNullOrWhiteSpace(deskripsi) ? null : deskripsi.Trim(),
            Status = ReportStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        _db.Reports.Add(report);
        await _db.SaveChangesAsync();

        return Json(new { success = true });
    }
}
