using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public enum RequestOfferStatus
{
    Pending,
    Accepted,
    Rejected
}

public class RequestOffer
{
    public int Id { get; set; }

    [Required]
    public int ItemRequestId { get; set; }

    [ForeignKey("ItemRequestId")]
    public ItemRequest? ItemRequest { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    [Required]
    public string Deskripsi { get; set; } = string.Empty;

    public RequestOfferStatus Status { get; set; } = RequestOfferStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ItemImage> Images { get; set; } = new List<ItemImage>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    public ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
}
