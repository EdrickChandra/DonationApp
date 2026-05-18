using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public enum ItemCondition
{
    Baru,
    Bekas
}

public enum ItemCategory
{
    Semua,
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

public class Item
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string NamaBarang { get; set; } = string.Empty;

    [Required]
    public ItemCategory Kategori { get; set; }

    [Required]
    public ItemCondition Kondisi { get; set; }

    [Required]
    [MaxLength(200)]
    public string Lokasi { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Provinsi { get; set; } = string.Empty;

    [Required]
    public string Deskripsi { get; set; } = string.Empty;

    public string? DetailTambahan { get; set; }

    public ItemStatus Status { get; set; } = ItemStatus.Available;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    public ICollection<ItemImage> Images { get; set; } = new List<ItemImage>();

    public ICollection<ClaimRequest> ClaimRequests { get; set; } = new List<ClaimRequest>();
}