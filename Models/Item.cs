using System.ComponentModel.DataAnnotations;

namespace DonationApp.Models;

public enum ItemCondition
{
    Baru,
    Bekas
}

public enum ItemCategory
{
    Pakaian,
    Elektronik,
    Buku,
    PerabotRumah,
    MainanHobi,
    AlatTulis,
    AlatMusik,
    PeralatanDapur
}

public enum ItemStatus
{
    Available,
    Claimed
}

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
