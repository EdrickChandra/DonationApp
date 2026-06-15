using DonationApp.Models;

namespace DonationApp.Services;

public static class CategoryHelper
{
    public static string DisplayName(ItemCategory category) => category switch
    {
        ItemCategory.Pakaian    => "Pakaian",
        ItemCategory.Elektronik => "Perangkat Elektronik",
        ItemCategory.PerabotRumah => "Perabot Rumah",
        ItemCategory.Mainan       => "Mainan",
        ItemCategory.AlatMusik  => "Alat Musik",
        _                       => category.ToString()
    };

    public static IEnumerable<ItemCategory> AllCategories() =>
    [
        ItemCategory.Pakaian,
        ItemCategory.Elektronik,
        ItemCategory.PerabotRumah,
        ItemCategory.Mainan,
        ItemCategory.AlatMusik
    ];
}
