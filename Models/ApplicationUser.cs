using Microsoft.AspNetCore.Identity;

namespace DonationApp.Models;

public class ApplicationUser : IdentityUser
{
    public string NamaDepan { get; set; } = string.Empty;
    public string NamaBelakang { get; set; } = string.Empty;
    public string NomorTelepon { get; set; } = string.Empty;
    public string Alamat { get; set; } = string.Empty;
}