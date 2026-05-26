using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public class ChatMessage
{
    public int Id { get; set; }

    [Required]
    public int ConversationId { get; set; }

    [ForeignKey("ConversationId")]
    public Conversation? Conversation { get; set; }

    [Required]
    public string SenderId { get; set; } = string.Empty;

    [ForeignKey("SenderId")]
    public ApplicationUser? Sender { get; set; }

    // Not [Required] — can be empty when message is image-only
    public string Content { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; } = false;
}
