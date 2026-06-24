using DonationApp.Models;

namespace DonationApp;

public static class HttpRequestExtensions
{

    public static bool IsAjaxRequest(this HttpRequest request)
        => request.Headers["X-Requested-With"] == "XMLHttpRequest";
}

public static class UserExtensions
{

    public static string GetFullName(this ApplicationUser? user, string fallback = "Seseorang")
        => user == null ? fallback : $"{user.NamaDepan} {user.NamaBelakang}";
}

public static class QuantityHelper
{

    public static int Clamp(int requested, int available) => Math.Max(1, Math.Min(requested, available));
}
