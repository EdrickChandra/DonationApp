using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public enum ImageOwnerType
{
    Donation,
    Request,
    RequestOffer
}

public class ItemImage
{
    public int Id { get; set; }

    [Required]
    public ImageOwnerType OwnerType { get; set; }

    // Only one of these will be set depending on OwnerType
    public int? ItemId { get; set; }
    public int? ItemRequestId { get; set; }
    public int? RequestOfferId { get; set; }

    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [ForeignKey("ItemRequestId")]
    public ItemRequest? ItemRequest { get; set; }

    [ForeignKey("RequestOfferId")]
    public RequestOffer? RequestOffer { get; set; }

    [Required]
    public string FilePath { get; set; } = string.Empty;
}
