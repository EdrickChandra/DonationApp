using DonationApp.Models;

namespace DonationApp.Services;

public static class CategoryHelper
{
    public static string DisplayName(ItemCategory category) => category switch
    {
        ItemCategory.Pakaian    => "Pakaian",
        ItemCategory.Elektronik => "Perangkat Elektronik",
        ItemCategory.Buku       => "Buku",
        ItemCategory.MainanHobi => "Mainan & Hobi",
        ItemCategory.AlatMusik  => "Alat Musik",
        _                       => category.ToString()
    };

    public static IEnumerable<ItemCategory> AllCategories() =>
    [
        ItemCategory.Pakaian,
        ItemCategory.Elektronik,
        ItemCategory.Buku,
        ItemCategory.MainanHobi,
        ItemCategory.AlatMusik
    ];
}
