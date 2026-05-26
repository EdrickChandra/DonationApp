using DonationApp.Models;

namespace DonationApp.Services;

public static class CategoryHelper
{
    public static string DisplayName(ItemCategory category) => category switch
    {
        ItemCategory.Pakaian        => "Pakaian",
        ItemCategory.Elektronik     => "Perangkat Elektronik",
        ItemCategory.Buku           => "Buku",
        ItemCategory.PerabotRumah   => "Perabot Rumah",
        ItemCategory.MainanHobi     => "Mainan & Hobi",
        ItemCategory.AlatTulis      => "Alat Tulis",
        ItemCategory.AlatMusik      => "Alat Musik",
        ItemCategory.PeralatanDapur => "Peralatan Dapur",
        _                           => category.ToString()
    };

    public static IEnumerable<ItemCategory> AllCategories() =>
    [
        ItemCategory.Pakaian,
        ItemCategory.Elektronik,
        ItemCategory.Buku,
        ItemCategory.PerabotRumah,
        ItemCategory.MainanHobi,
        ItemCategory.AlatTulis,
        ItemCategory.AlatMusik,
        ItemCategory.PeralatanDapur
    ];
}
