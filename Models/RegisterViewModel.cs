using System.ComponentModel.DataAnnotations;

namespace DonationApp.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Nama depan wajib diisi.")]
    public string NamaDepan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nama belakang wajib diisi.")]
    public string NamaBelakang { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email wajib diisi.")]
    [EmailAddress(ErrorMessage = "Format email tidak valid.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nomor telepon wajib diisi.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Alamat wajib diisi.")]
    public string Alamat { get; set; } = string.Empty;

    [Required(ErrorMessage = "Provinsi wajib diisi.")]
    public string Provinsi { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kota wajib diisi.")]
    public string Kota { get; set; } = string.Empty;

    [RegularExpression(@"^\d{5}$", ErrorMessage = "Kode pos harus 5 digit angka.")]
    public string? KodePos { get; set; }

    [Required(ErrorMessage = "Password wajib diisi.")]
    [MinLength(6, ErrorMessage = "Password minimal 6 karakter.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Konfirmasi password wajib diisi.")]
    [Compare("Password", ErrorMessage = "Password tidak cocok.")]
    public string KonfirmasiPassword { get; set; } = string.Empty;
}
