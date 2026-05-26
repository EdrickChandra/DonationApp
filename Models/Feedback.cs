using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public class Feedback
{
    public int Id { get; set; }

    [Required]
    public string ReviewerId { get; set; } = string.Empty;

    [Required]
    public string ReviewedUserId { get; set; } = string.Empty;

    [ForeignKey("ReviewerId")]
    public ApplicationUser? Reviewer { get; set; }

    [ForeignKey("ReviewedUserId")]
    public ApplicationUser? ReviewedUser { get; set; }

    // Only one of these will be set depending on which flow triggered the review
    public int? ClaimRequestId { get; set; }
    public int? RequestOfferId { get; set; }

    [ForeignKey("ClaimRequestId")]
    public ClaimRequest? ClaimRequest { get; set; }

    [ForeignKey("RequestOfferId")]
    public RequestOffer? RequestOffer { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    public string? Komentar { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
