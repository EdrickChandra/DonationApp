using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public abstract class ListingBase
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    [Required]
    public ItemCategory Kategori { get; set; }

    [Required]
    [MaxLength(200)]
    public string Lokasi { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Provinsi { get; set; } = string.Empty;

    [Required]
    public string Deskripsi { get; set; } = string.Empty;

    public string? DetailTambahan { get; set; }

    public int Jumlah { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }
}
