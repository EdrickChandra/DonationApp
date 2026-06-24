using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public class AdminAction
{
    public int Id { get; set; }

    [Required]
    public string AdminId { get; set; } = string.Empty;

    [ForeignKey("AdminId")]
    public ApplicationUser? Admin { get; set; }

    public int? ReportId { get; set; }

    [ForeignKey("ReportId")]
    public Report? Report { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
