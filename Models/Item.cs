using System.ComponentModel.DataAnnotations;

namespace DonationApp.Models;

public enum ItemCondition
{
    Baru,
    Bekas,
    PerluPerbaikan
}

public enum ItemCategory
{
    Semua,
    Pakaian,
    Elektronik,
    Buku
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

    [Required]
    public string Deskripsi { get; set; } = string.Empty;

    public ItemStatus Status { get; set; } = ItemStatus.Available;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
}
