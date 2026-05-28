using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public class Notification
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    public string? RefId { get; set; }

    public bool IsRead { get; set; } = false;

    public string? Link { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
