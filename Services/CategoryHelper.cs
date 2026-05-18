using DonationApp.Models;

namespace DonationApp.Services;

public static class CategoryHelper
{
    public static string DisplayName(ItemCategory category) => category switch
    {
        ItemCategory.Semua => "Semua",
        ItemCategory.Pakaian => "Pakaian",
        ItemCategory.Elektronik => "Elektronik",
        ItemCategory.Buku => "Buku",
        ItemCategory.PerabotRumah => "Perabot Rumah",
        ItemCategory.MainanHobi => "Mainan & Hobi",
        ItemCategory.AlatTulis => "Alat Tulis",
        ItemCategory.AlatMusik => "Alat Musik",
        ItemCategory.PeralatanDapur => "Peralatan Dapur",
        _ => category.ToString()
    };

    public static IEnumerable<ItemCategory> AllCategories()
        => Enum.GetValues<ItemCategory>();
}