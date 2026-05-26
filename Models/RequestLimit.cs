using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public class RequestLimit
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    public int RequestCount { get; set; } = 0;

    public DateTime PeriodStart { get; set; } = DateTime.UtcNow;

    // PeriodEnd is not stored — derive it as PeriodStart.AddDays(N) in code
}
