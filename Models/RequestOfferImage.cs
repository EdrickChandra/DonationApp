using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public class RequestOfferImage
{
    public int Id { get; set; }

    [Required]
    public int RequestOfferId { get; set; }

    [ForeignKey("RequestOfferId")]
    public RequestOffer? RequestOffer { get; set; }

    [Required]
    public string FilePath { get; set; } = string.Empty;
}