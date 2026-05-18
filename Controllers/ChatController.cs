using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Controllers;

[Authorize]
public class ChatController : BaseController
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public ChatController(AppDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        : base(context, userManager)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> GetMessages(int conversationId)
    {
        var userId = _userManager.GetUserId(User)!;

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId &&
                (c.RequesterId == userId || c.DonorId == userId));

        if (conversation == null)
            return Forbid();

        var messages = await _context.ChatMessages
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

        var unread = await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead)
            .ToListAsync();

        foreach (var msg in unread)
            msg.IsRead = true;

        await _context.SaveChangesAsync();

        return Json(messages);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(int conversationId, IFormFile image)
    {
        var userId = _userManager.GetUserId(User)!;

        var conversation = await _context.Conversations
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

        var count = await _context.ChatMessages
            .Where(m => m.SenderId != userId && !m.IsRead &&
                _context.Conversations.Any(c => c.Id == m.ConversationId &&
                    (c.RequesterId == userId || c.DonorId == userId)))
            .CountAsync();

        return Json(count);
    }
}