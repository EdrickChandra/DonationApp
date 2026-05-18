using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public class UserReputation
{
    public int Id { get; set; }

    [Required]
    public string ReviewerId { get; set; } = string.Empty;

    [ForeignKey("ReviewerId")]
    public ApplicationUser? Reviewer { get; set; }

    [Required]
    public string ReviewedUserId { get; set; } = string.Empty;

    [ForeignKey("ReviewedUserId")]
    public ApplicationUser? ReviewedUser { get; set; }

    [Required]
    public int ClaimRequestId { get; set; }

    [ForeignKey("ClaimRequestId")]
    public ClaimRequest? ClaimRequest { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    public string? Komentar { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}