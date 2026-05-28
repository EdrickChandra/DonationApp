using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public class Report
{
    public int Id { get; set; }

    [Required]
    public string ReporterId { get; set; } = string.Empty;

    [ForeignKey("ReporterId")]
    public ApplicationUser? Reporter { get; set; }

    public string? TargetUserId { get; set; }
    public int? TargetDonationId { get; set; }
    public int? TargetRequestId { get; set; }

    [ForeignKey("TargetUserId")]
    public ApplicationUser? TargetUser { get; set; }

    [ForeignKey("TargetDonationId")]
    public Item? TargetDonation { get; set; }

    [ForeignKey("TargetRequestId")]
    public ItemRequest? TargetRequest { get; set; }

    [Required]
    public ReportReason Alasan { get; set; }

    public string? Deskripsi { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public ICollection<AdminAction> AdminActions { get; set; } = new List<AdminAction>();
}
