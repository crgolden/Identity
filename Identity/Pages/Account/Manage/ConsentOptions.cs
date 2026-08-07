namespace Identity.Pages.Account.Manage;

public static class ConsentOptions
{
    public static bool EnableOfflineAccess { get; } = true;

    public static string OfflineAccessDisplayName { get; } = "Offline Access";

    public static string OfflineAccessDescription { get; } =
        "Access to your applications and resources, even when you are offline.";

    public static string MustChooseOneErrorMessage { get; } = "You must pick at least one permission.";

    public static string InvalidSelectionErrorMessage { get; } = "Invalid selection.";
}