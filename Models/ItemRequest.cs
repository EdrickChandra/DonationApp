using System.ComponentModel.DataAnnotations;

namespace DonationApp.Models;

public class ItemRequest : ListingBase
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public ItemCondition? KondisiMinimum { get; set; }

    public ItemRequestStatus Status { get; set; } = ItemRequestStatus.Open;

    public ICollection<ItemImage> Images { get; set; } = new List<ItemImage>();
    public ICollection<RequestOffer> Offers { get; set; } = new List<RequestOffer>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}