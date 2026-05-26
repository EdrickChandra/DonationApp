using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public enum ClaimRequestStatus
{
    Pending,
    Accepted,
    Rejected,
    Shipped,
    Delivered
}

public class ClaimRequest
{
    public int Id { get; set; }

    [Required]
    public int ItemId { get; set; }

    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    public ClaimRequestStatus Status { get; set; } = ClaimRequestStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    public ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
}
