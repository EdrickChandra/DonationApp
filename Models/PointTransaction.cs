using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public class PointTransaction
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    [Required]
    public PointTransactionType Type { get; set; }

    public int? ClaimRequestId { get; set; }
    public int? RequestOfferId { get; set; }

    [ForeignKey("ClaimRequestId")]
    public ClaimRequest? ClaimRequest { get; set; }

    [ForeignKey("RequestOfferId")]
    public RequestOffer? RequestOffer { get; set; }

    public int Amount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
