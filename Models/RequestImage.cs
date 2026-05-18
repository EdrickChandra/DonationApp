using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public class RequestImage
{
    public int Id { get; set; }

    [Required]
    public int ItemRequestId { get; set; }

    [ForeignKey("ItemRequestId")]
    public ItemRequest? ItemRequest { get; set; }

    [Required]
    public string FilePath { get; set; } = string.Empty;
}