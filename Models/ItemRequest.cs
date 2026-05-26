using System.ComponentModel.DataAnnotations;

namespace DonationApp.Models;

public enum ItemRequestStatus
{
    Open,
    Fulfilled,
    Expired,
    Closed
}

public class ItemRequest : ListingBase
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    // Replaces KondisiMinimum enum — null means either condition is acceptable
    public ItemCondition? KondisiMinimum { get; set; }

    public ItemRequestStatus Status { get; set; } = ItemRequestStatus.Open;

    public ICollection<ItemImage> Images { get; set; } = new List<ItemImage>();
    public ICollection<RequestOffer> Offers { get; set; } = new List<RequestOffer>();
}
