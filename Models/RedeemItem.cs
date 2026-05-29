using System.ComponentModel.DataAnnotations;

namespace DonationApp.Models;

public class RedeemItem
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int PointCost { get; set; }

    public string? ImageUrl { get; set; }

    public int Stock { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RedeemTransaction> RedeemTransactions { get; set; } = new List<RedeemTransaction>();
}