using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using DonationApp.Data;
using DonationApp.Models;

namespace DonationApp.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _context;

    public ChatHub(AppDbContext context)
    {
        _context = context;
    }

    public async Task JoinConversation(int conversationId)
    {
        var userId = Context.UserIdentifier;

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId &&
                (c.RequesterId == userId || c.DonorId == userId));

        if (conversation == null) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"conv_{conversationId}");

        var unread = await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead)
            .ToListAsync();

        foreach (var msg in unread)
            msg.IsRead = true;

        await _context.SaveChangesAsync();
    }

    public async Task SendMessage(int conversationId, string content, string? imagePath = null)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(imagePath)) return;

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId &&
                (c.RequesterId == userId || c.DonorId == userId));

        if (conversation == null) return;

        var message = new ChatMessage
        {
            ConversationId = conversationId,
            SenderId = userId!,
            Content = content?.Trim() ?? "",
            SentAt = DateTime.UtcNow,
            IsRead = false,
            ImagePath = imagePath
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        var sender = await _context.Users.FindAsync(userId);
        var senderName = sender != null ? sender.NamaDepan + " " + sender.NamaBelakang : "Unknown";

        await Clients.Group($"conv_{conversationId}").SendAsync("ReceiveMessage", new
        {
            id = message.Id,
            conversationId = message.ConversationId,
            senderId = message.SenderId,
            senderName,
            content = message.Content,
            sentAt = message.SentAt.ToString("o"),
            isRead = message.IsRead,
            imagePath = message.ImagePath
        });
    }

    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
    }
}