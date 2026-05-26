using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Controllers;

[Authorize]
public class PesanController : AppBaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public PesanController(AppDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        : base(db, userManager)
    {
        _userManager = userManager;
        _environment = environment;
    }

    // -------------------------------------------------------------------------
    // Page view (was PesanController)
    // -------------------------------------------------------------------------

    public async Task<IActionResult> Index(int? convId)
    {
        var userId = _userManager.GetUserId(User)!;

        var conversations = await _db.Conversations
            .Include(c => c.Item)
                .ThenInclude(i => i!.Images)
            .Include(c => c.Requester)
            .Include(c => c.Donor)
            .Where(c => c.RequesterId == userId || c.DonorId == userId)
            .ToListAsync();

        var conversationIds = conversations.Select(c => c.Id).ToList();

        var lastMessages = await _db.ChatMessages
            .Where(m => conversationIds.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => g.OrderByDescending(m => m.SentAt).First())
            .ToListAsync();

        var lastMessageMap = lastMessages.ToDictionary(m => m.ConversationId);

        foreach (var conv in conversations)
        {
            conv.Messages = lastMessageMap.TryGetValue(conv.Id, out var last)
                ? new List<ChatMessage> { last }
                : new List<ChatMessage>();
        }

        conversations = conversations
            .OrderByDescending(c => lastMessageMap.TryGetValue(c.Id, out var m) ? m.SentAt : c.CreatedAt)
            .ToList();

        ViewBag.Conversations = conversations;
        ViewBag.CurrentUserId = userId;
        ViewBag.InitialConvId = convId;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("~/Views/Profile/Pesan/Pesan.cshtml");

        ViewBag.InitialSection = "pesan";
        return View("~/Views/Profile/Shell.cshtml");
    }

    // -------------------------------------------------------------------------
    // AJAX / API actions (was ChatController)
    // -------------------------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> GetMessages(int conversationId)
    {
        var userId = _userManager.GetUserId(User)!;

        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId &&
                (c.RequesterId == userId || c.DonorId == userId));

        if (conversation == null)
            return Forbid();

        var messages = await _db.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.SentAt)
            .Select(m => new
            {
                m.Id,
                m.ConversationId,
                m.SenderId,
                m.Content,
                SentAt = m.SentAt.ToString("o"),
                m.IsRead,
                m.ImagePath
            })
            .ToListAsync();

        var unread = await _db.ChatMessages
            .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead)
            .ToListAsync();

        foreach (var msg in unread)
            msg.IsRead = true;

        await _db.SaveChangesAsync();

        return Json(messages);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(int conversationId, IFormFile image)
    {
        var userId = _userManager.GetUserId(User)!;

        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId &&
                (c.RequesterId == userId || c.DonorId == userId));

        if (conversation == null)
            return Json(new { success = false, error = "Conversation not found" });

        if (image == null || image.Length == 0)
            return Json(new { success = false, error = "No image provided" });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(image.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
            return Json(new { success = false, error = "Invalid file type" });

        if (image.Length > 5 * 1024 * 1024)
            return Json(new { success = false, error = "File too large" });

        var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "chat", conversationId.ToString());
        Directory.CreateDirectory(uploadFolder);

        var fileName = Guid.NewGuid().ToString() + extension;
        var filePath = Path.Combine(uploadFolder, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await image.CopyToAsync(stream);

        var relativePath = $"/uploads/chat/{conversationId}/{fileName}";
        return Json(new { success = true, path = relativePath });
    }

    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = _userManager.GetUserId(User)!;

        var count = await _db.ChatMessages
            .Where(m => m.SenderId != userId && !m.IsRead &&
                _db.Conversations.Any(c => c.Id == m.ConversationId &&
                    (c.RequesterId == userId || c.DonorId == userId)))
            .CountAsync();

        return Json(count);
    }
}
