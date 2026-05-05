using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace DonationApp.Controllers;

[Authorize]
public class ItemController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public ItemController(AppDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Item item, List<IFormFile> Images)
    {
        ModelState.Remove("UserId");
        ModelState.Remove("User");
        ModelState.Remove("Images");

        if (Images == null || Images.Count == 0)
        {
            ModelState.AddModelError("Images", "Minimal 1 foto harus diunggah.");
            return View(item);
        }

        if (!ModelState.IsValid)
            return View(item);

        var userId = _userManager.GetUserId(User)!;

        item.CreatedAt = DateTime.UtcNow;
        item.ExpiresAt = DateTime.UtcNow.AddDays(7);
        item.Status = ItemStatus.Available;
        item.UserId = userId;

        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", userId, item.Id.ToString());
        Directory.CreateDirectory(uploadFolder);

        int imageCount = 0;
        foreach (var image in Images)
        {
            if (imageCount >= 5) break;

            var extension = Path.GetExtension(image.FileName).ToLower();
            if (!allowedExtensions.Contains(extension)) continue;
            if (image.Length > 5 * 1024 * 1024) continue;

            var fileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(stream);

            _context.ItemImages.Add(new ItemImage
            {
                ItemId = item.Id,
                FilePath = $"/uploads/{userId}/{item.Id}/{fileName}"
            });

            imageCount++;
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Index", "Profile", new { section = "overview" });
    }

    [AllowAnonymous]
    public async Task<IActionResult> Detail(int id)
    {
        var userId = _userManager.GetUserId(User);

        var item = await _context.Items
            .Include(i => i.Images)
            .Include(i => i.ClaimRequests)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null)
            return NotFound();

        ViewBag.IsOwner = userId == item.UserId;
        ViewBag.HasRequested = userId != null && item.ClaimRequests.Any(r => r.UserId == userId);

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Request(int itemId)
    {
        var userId = _userManager.GetUserId(User)!;

        var item = await _context.Items.FindAsync(itemId);
        if (item == null || item.UserId == userId || item.Status != ItemStatus.Available)
            return RedirectToAction("Detail", new { id = itemId });

        var existing = await _context.ClaimRequests
            .AnyAsync(r => r.ItemId == itemId && r.UserId == userId);

        if (!existing)
        {
            _context.ClaimRequests.Add(new ClaimRequest
            {
                ItemId = itemId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Detail", new { id = itemId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int requestId)
    {
        var userId = _userManager.GetUserId(User)!;

        var request = await _context.ClaimRequests
            .Include(r => r.Item)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null || request.Item == null || request.Item.UserId != userId)
            return RedirectToAction("Index", "Profile", new { section = "permintaan" });

        request.Status = ClaimRequestStatus.Accepted;
        request.Item.Status = ItemStatus.Claimed;

        await _context.SaveChangesAsync();
        return RedirectToAction("Index", "Profile", new { section = "permintaan" });
    }

}   