using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public class Conversation
{
    public int Id { get; set; }

    [Required]
    public int ItemId { get; set; }

    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Required]
    public int ClaimRequestId { get; set; }

    [ForeignKey("ClaimRequestId")]
    public ClaimRequest? ClaimRequest { get; set; }

    [Required]
    public string RequesterId { get; set; } = string.Empty;

    [ForeignKey("RequesterId")]
    public ApplicationUser? Requester { get; set; }

    [Required]
    public string DonorId { get; set; } = string.Empty;

    [ForeignKey("DonorId")]
    public ApplicationUser? Donor { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}