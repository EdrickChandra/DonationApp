using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public class ItemImage
{
    public int Id { get; set; }

    [Required]
    public int ItemId { get; set; }

    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Required]
    public string FilePath { get; set; } = string.Empty;
}