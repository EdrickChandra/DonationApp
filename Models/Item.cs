using System.ComponentModel.DataAnnotations;

namespace DonationApp.Models;

public class Item : ListingBase
{
    [Required]
    [MaxLength(200)]
    public string NamaBarang { get; set; } = string.Empty;

    [Required]
    public ItemCondition Kondisi { get; set; }

    public ItemStatus Status { get; set; } = ItemStatus.Available;

    public ICollection<ItemImage> Images { get; set; } = new List<ItemImage>();
    public ICollection<ClaimRequest> ClaimRequests { get; set; } = new List<ClaimRequest>();
}
