using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public enum NotificationType
{
    ClaimRequest,
    ClaimAccepted,
    ClaimRejected,
    ItemShipped,
    ItemDelivered,
    NewOffer,
    OfferAccepted,
    OfferRejected,
    NewRating,
    Report,
    PointEarned
}

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

    // ID of the related entity (e.g. ItemId, ClaimRequestId) for deep linking
    public string? RefId { get; set; }

    public bool IsRead { get; set; } = false;

    public string? Link { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
