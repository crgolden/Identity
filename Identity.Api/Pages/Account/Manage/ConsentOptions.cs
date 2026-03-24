namespace Identity.Pages.Account.Manage;

/// <summary>Configuration constants for the OAuth2 consent and device authorization UI pages.</summary>
public static class ConsentOptions
{
    /// <summary>Whether the offline access (refresh token) scope is shown in the consent UI.</summary>
    public static bool EnableOfflineAccess { get; } = true;

    /// <summary>Display name shown for the offline access scope.</summary>
    public static string OfflineAccessDisplayName { get; } = "Offline Access";

    /// <summary>Description shown for the offline access scope.</summary>
    public static string OfflineAccessDescription { get; } =
        "Access to your applications and resources, even when you are offline.";

    /// <summary>Error message shown when the user submits the consent form without selecting any scope.</summary>
    public static string MustChooseOneErrorMessage { get; } = "You must pick at least one permission.";

    /// <summary>Error message shown when an unexpected button value is submitted.</summary>
    public static string InvalidSelectionErrorMessage { get; } = "Invalid selection.";
}
