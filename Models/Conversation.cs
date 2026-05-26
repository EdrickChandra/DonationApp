using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public enum ConversationType
{
    Donation,
    RequestOffer
}

public class Conversation
{
    public int Id { get; set; }

    [Required]
    public ConversationType Type { get; set; }

    // Populated when Type = Donation
    public int? ItemId { get; set; }
    public int? ClaimRequestId { get; set; }

    // Populated when Type = RequestOffer
    public int? RequestOfferId { get; set; }

    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [ForeignKey("ClaimRequestId")]
    public ClaimRequest? ClaimRequest { get; set; }

    [ForeignKey("RequestOfferId")]
    public RequestOffer? RequestOffer { get; set; }

    [Required]
    public string RequesterId { get; set; } = string.Empty;

    [Required]
    public string DonorId { get; set; } = string.Empty;

    [ForeignKey("RequesterId")]
    public ApplicationUser? Requester { get; set; }

    [ForeignKey("DonorId")]
    public ApplicationUser? Donor { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
