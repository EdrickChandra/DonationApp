using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public enum ItemRequestStatus
{
    Open,
    Fulfilled,
    Expired,
    Closed
}

public enum KondisiMinimum
{
    Baru,
    Bekas,
    Keduanya
}

public class ItemRequest
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public ItemCategory Kategori { get; set; }

    [Required]
    public string Deskripsi { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Lokasi { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Provinsi { get; set; } = string.Empty;

    public KondisiMinimum KondisiMinimum { get; set; } = KondisiMinimum.Keduanya;

    public int Jumlah { get; set; } = 1;

    public string? DetailTambahan { get; set; }

    public ItemRequestStatus Status { get; set; } = ItemRequestStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(14);

    public ICollection<RequestImage> Images { get; set; } = new List<RequestImage>();

    public ICollection<RequestOffer> Offers { get; set; } = new List<RequestOffer>();
}