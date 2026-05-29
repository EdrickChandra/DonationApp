using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public class RedeemTransaction
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    [Required]
    public int RedeemItemId { get; set; }

    [ForeignKey("RedeemItemId")]
    public RedeemItem? RedeemItem { get; set; }

    public int PointsSpent { get; set; }

    public RedeemStatus Status { get; set; } = RedeemStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum RedeemStatus
{
    Pending,
    Fulfilled,
    Cancelled
}