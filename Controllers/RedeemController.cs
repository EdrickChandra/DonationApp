using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;
using DonationApp.Services;

namespace DonationApp.Controllers;

[Authorize]
public class RedeemController : AppBaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PointsService _points;

    public RedeemController(AppDbContext db, UserManager<ApplicationUser> userManager, PointsService points)
        : base(db, userManager)
    {
        _userManager = userManager;
        _points = points;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.FindByIdAsync(userId);

        var items = await _db.RedeemItems
            .Where(i => i.IsActive)
            .OrderBy(i => i.PointCost)
            .ToListAsync();

        var pointHistory = await _db.PointTransactions
            .Include(p => p.RedeemItem)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(20)
            .ToListAsync();

        ViewBag.UserPoints = user?.TotalPoin ?? 0;
        ViewBag.RedeemItems = items;
        ViewBag.RedeemHistory = pointHistory.Where(p => p.Type == PointTransactionType.SpendPoint).ToList();
        ViewBag.PointHistory = pointHistory;

        if (Request.IsAjaxRequest())
            return PartialView("~/Views/Profile/Redeem/Redeem.cshtml");

        ViewBag.InitialSection = "redeem";
        return View("~/Views/Profile/Shell.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Redeem(int redeemItemId)
    {
        var userId = _userManager.GetUserId(User)!;
        var error = await _points.RedeemAsync(userId, redeemItemId);

        if (error != null)
            TempData["RedeemError"] = error;
        else
            TempData["RedeemSuccess"] = "Penukaran berhasil! Item akan segera diproses.";

        return RedirectToAction("Index");
    }
}