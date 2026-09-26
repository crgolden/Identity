namespace Identity.Pages.Account.Manage;

using Microsoft.AspNetCore.Mvc.Rendering;

public static class ManageNavPages
{
    internal const string ActivePageViewDataKey = "ActivePage";

    internal const string ActiveNavClass = "active";

    public static string Index => "Index";

    public static string Email => "Email";

    public static string ChangePassword => "ChangePassword";

    public static string DownloadPersonalData => "DownloadPersonalData";

    public static string DeletePersonalData => "DeletePersonalData";

    public static string ExternalLogins => "ExternalLogins";

    public static string PersonalData => "PersonalData";

    public static string TwoFactorAuthentication => "TwoFactorAuthentication";

    public static string Passkeys => "Passkeys";

    public static string Grants => "Grants";

    public static string? PageNavClass(ViewContext? viewContext, string page)
    {
        ThrowIfNull(viewContext);

        var activePage = viewContext.ViewData[ActivePageViewDataKey] as string ?? Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);
        return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? ActiveNavClass : null;
    }
}
