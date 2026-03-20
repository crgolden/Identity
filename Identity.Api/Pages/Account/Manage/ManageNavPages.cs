namespace Identity.Pages.Account.Manage;

using Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>Provides page name constants and active-CSS-class helpers for the account management navigation menu.</summary>
public static class ManageNavPages
{
    public static string Index => "Index";

    public static string Email => "Email";

    public static string ChangePassword => "ChangePassword";

    public static string DownloadPersonalData => "DownloadPersonalData";

    public static string DeletePersonalData => "DeletePersonalData";

    public static string ExternalLogins => "ExternalLogins";

    public static string PersonalData => "PersonalData";

    public static string TwoFactorAuthentication => "TwoFactorAuthentication";

    public static string Passkeys => "Passkeys";

    /// <inheritdoc cref="PageNavClass"/>
    public static string? IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);

    /// <inheritdoc cref="PageNavClass"/>
    public static string? EmailNavClass(ViewContext viewContext) => PageNavClass(viewContext, Email);

    /// <inheritdoc cref="PageNavClass"/>
    public static string? ChangePasswordNavClass(ViewContext viewContext) => PageNavClass(viewContext, ChangePassword);

    /// <inheritdoc cref="PageNavClass"/>
    public static string? DownloadPersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, DownloadPersonalData);

    /// <inheritdoc cref="PageNavClass"/>
    public static string? DeletePersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, DeletePersonalData);

    /// <inheritdoc cref="PageNavClass"/>
    public static string? ExternalLoginsNavClass(ViewContext viewContext) => PageNavClass(viewContext, ExternalLogins);

    /// <inheritdoc cref="PageNavClass"/>
    public static string? PersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, PersonalData);

    /// <inheritdoc cref="PageNavClass"/>
    public static string? TwoFactorAuthenticationNavClass(ViewContext viewContext) => PageNavClass(viewContext, TwoFactorAuthentication);

    /// <inheritdoc cref="PageNavClass"/>
    public static string? PasskeysNavClass(ViewContext viewContext) => PageNavClass(viewContext, Passkeys);

    /// <summary>Returns <c>"active"</c> if the current view matches <paramref name="page"/>, otherwise <see langword="null"/>.</summary>
    /// <param name="viewContext">The current view context used to determine the active page.</param>
    /// <param name="page">The page name to compare against the active page.</param>
    /// <returns><c>"active"</c> when the current page matches <paramref name="page"/>; otherwise <see langword="null"/>.</returns>
    public static string? PageNavClass(ViewContext viewContext, string page)
    {
        var activePage = viewContext.ViewData["ActivePage"] as string ?? Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);
        return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
    }
}
