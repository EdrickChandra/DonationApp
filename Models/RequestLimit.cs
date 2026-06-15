using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonationApp.Models;

public class RequestLimit
{
    public const int MaxRequestsPerWeek = 3;

    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    public int RequestCount { get; set; } = 0;

    public DateTime PeriodStart { get; set; } = DateTime.UtcNow;

    public void Increment()
    {
        ResetIfNewPeriod();
        RequestCount++;
    }

    public bool IsLimitReached()
    {
        ResetIfNewPeriod();
        return RequestCount >= MaxRequestsPerWeek;
    }

    public DateTime GetPeriodEnd()
    {
        return PeriodStart.AddDays(7);
    }

    private void ResetIfNewPeriod()
    {
        if (DateTime.UtcNow >= GetPeriodEnd())
        {
            RequestCount = 0;
            PeriodStart = DateTime.UtcNow;
        }
    }
}
