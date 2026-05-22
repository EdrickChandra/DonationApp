using DonationApp.Models;

namespace DonationApp.Services;

public static class CategoryHelper
{
    public static string DisplayName(ItemCategory category) => category switch
    {
        ItemCategory.Semua => "Semua",
        ItemCategory.Pakaian => "Pakaian",
        ItemCategory.Elektronik => "Perangkat Elektronik",
        ItemCategory.Buku => "Buku",
        ItemCategory.MainanHobi => "Mainan Anak",
        ItemCategory.AlatMusik => "Alat Musik",
        _ => category.ToString()
    };

    public static IEnumerable<ItemCategory> AllCategories()
        => new[]
        {
            ItemCategory.Semua,
            ItemCategory.Pakaian,
            ItemCategory.Elektronik,
            ItemCategory.Buku,
            ItemCategory.MainanHobi,
            ItemCategory.AlatMusik
        };
}