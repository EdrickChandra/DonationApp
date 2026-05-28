using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

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

    [MaxLength(200)]
    public string NamaBarang { get; set; } = string.Empty;

    public ItemCondition Kondisi { get; set; } = ItemCondition.Bekas;

    [MaxLength(200)]
    public string Lokasi { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Provinsi { get; set; } = string.Empty;

    [Range(1, 999)]
    public int Jumlah { get; set; } = 1;

    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ItemImage> Images { get; set; } = new List<ItemImage>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    public ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
}