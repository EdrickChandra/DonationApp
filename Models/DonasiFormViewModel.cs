using System.ComponentModel.DataAnnotations;

namespace DonationApp.Models;

public class DonasiFormViewModel
{
    [Required(ErrorMessage = "Nama barang wajib diisi.")]
    [MaxLength(200)]
    public string NamaBarang { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategori wajib dipilih.")]
    public ItemCategory Kategori { get; set; }

    public ItemCondition Kondisi { get; set; } = ItemCondition.Baru;

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
