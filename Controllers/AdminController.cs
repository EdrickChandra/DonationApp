using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DonationApp.Controllers;

[Authorize]
public class AdminController : AppBaseController
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(AppDbContext db, UserManager<ApplicationUser> userManager)
        : base(db, userManager)
    {
        _userManager = userManager;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await base.OnActionExecutionAsync(context, next);
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.IsAdmin)
        {
            context.Result = Forbid();
            return;
        }
        await next();
    }

    public async Task<IActionResult> Reports(ReportStatus? status)
    {
        var query = _db.Reports
            .Include(r => r.Reporter)
            .Include(r => r.TargetUser)
            .Include(r => r.TargetDonation)
            .Include(r => r.TargetRequest)
            .Include(r => r.AdminActions)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var reports = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        ViewBag.SelectedStatus = status;
        return View("~/Views/Admin/Reports.cshtml", reports);
    }

    public async Task<IActionResult> ReportDetail(int id)
    {
        var report = await _db.Reports
            .Include(r => r.Reporter)
            .Include(r => r.TargetUser)
            .Include(r => r.TargetDonation)
                .ThenInclude(d => d!.Images)
            .Include(r => r.TargetRequest)
                .ThenInclude(req => req!.Images)
            .Include(r => r.AdminActions)
                .ThenInclude(a => a.Admin)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null) return NotFound();

        return View("~/Views/Admin/ReportDetail.cshtml", report);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TakeAction(int reportId, string action, string? note)
    {
        var adminId = _userManager.GetUserId(User)!;

        var report = await _db.Reports
            .Include(r => r.TargetUser)
            .Include(r => r.TargetDonation)
            .Include(r => r.TargetRequest)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null) return NotFound();

        if (action == "Reviewing" && report.Status != ReportStatus.Open)
            return RedirectToAction("ReportDetail", new { id = reportId });

        if ((action == "Dismiss" || action == "BanUser" || action == "WarnUser" || action == "RemoveItems")
            && (report.Status == ReportStatus.Resolved || report.Status == ReportStatus.Dismissed))
            return RedirectToAction("ReportDetail", new { id = reportId });

        var adminAction = new AdminAction
        {
            AdminId = adminId,
            ReportId = reportId,
            Action = action,
            Note = note?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.AdminActions.Add(adminAction);

        switch (action)
        {
            case "BanUser":
                if (report.TargetUser != null)
                {
                    report.TargetUser.IsBanned = true;
                    await _userManager.UpdateAsync(report.TargetUser);
                    await SendNotificationAsync(report.TargetUser.Id, "Akun Anda telah dinonaktifkan karena melanggar ketentuan layanan.");
                }
                report.Status = ReportStatus.Resolved;
                report.ResolvedAt = DateTime.UtcNow;
                break;

            case "WarnUser":
                if (report.TargetUser != null)
                    await SendNotificationAsync(report.TargetUser.Id, "Anda mendapat peringatan dari admin. Harap patuhi ketentuan layanan.");
                report.Status = ReportStatus.Resolved;
                report.ResolvedAt = DateTime.UtcNow;
                break;

            case "RemoveItems":
                if (report.TargetDonation != null)
                {
                    report.TargetDonation.Status = ItemStatus.Claimed;
                    if (report.TargetUser != null)
                        await SendNotificationAsync(report.TargetUser.Id, "Salah satu item donasi Anda telah dihapus oleh admin.");
                }
                if (report.TargetRequest != null)
                {
                    report.TargetRequest.Status = ItemRequestStatus.Closed;
                    if (report.TargetUser != null)
                        await SendNotificationAsync(report.TargetUser.Id, "Salah satu request Anda telah dihapus oleh admin.");
                }
                report.Status = ReportStatus.Resolved;
                report.ResolvedAt = DateTime.UtcNow;
                break;

            case "Dismiss":
                report.Status = ReportStatus.Dismissed;
                report.ResolvedAt = DateTime.UtcNow;
                break;

            case "Reviewing":
                report.Status = ReportStatus.Reviewing;
                break;
        }

        await _db.SaveChangesAsync();
        TempData["AdminSuccess"] = "Aksi berhasil dilakukan.";
        return RedirectToAction("ReportDetail", new { id = reportId });
    }

    private async Task SendNotificationAsync(string userId, string message)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Message = message,
            Type = NotificationType.Report,
            Link = "/Profile/Overview",
            CreatedAt = DateTime.UtcNow
        });
        await Task.CompletedTask;
    }
}