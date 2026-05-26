using Microsoft.AspNetCore.Identity;

namespace DonationApp.Models;

public class ApplicationUser : IdentityUser
{
    public string NamaDepan { get; set; } = string.Empty;
    public string NamaBelakang { get; set; } = string.Empty;
    public string Alamat { get; set; } = string.Empty;
    public string Provinsi { get; set; } = string.Empty;
    public string? KodePos { get; set; }
    public decimal TrustScore { get; set; } = 0;
    public int TotalPoin { get; set; } = 0;
    public bool IsAdmin { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}
