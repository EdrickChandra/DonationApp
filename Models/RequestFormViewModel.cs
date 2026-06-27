using System.ComponentModel.DataAnnotations;

namespace DonationApp.Models;

// Carries exactly the fields the request form (BuatRequest.cshtml) submits.
// Binding to this instead of the ItemRequest entity avoids over-posting and
// removes the need for ModelState.Remove(...) on server-controlled/navigation
// properties.
public class RequestFormViewModel
{
    [Required(ErrorMessage = "Judul request wajib diisi.")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategori wajib dipilih.")]
    public ItemCategory Kategori { get; set; }

    public ItemCondition? KondisiMinimum { get; set; }

    [Required(ErrorMessage = "Kota wajib dipilih.")]
    [MaxLength(200)]
    public string Lokasi { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Provinsi { get; set; }

    public int Jumlah { get; set; } = 1;

    [Required(ErrorMessage = "Deskripsi wajib diisi.")]
    public string Deskripsi { get; set; } = string.Empty;

    public string? DetailTambahanJson { get; set; }
}
