namespace Identity.Pages.Account.Manage;

public sealed record ManageSection(string NavId, string Title, string Page, bool RequiresExternalLogins)
{
    public const string LinkIdPrefix = "manage-section-";

    public string AspPage => $"./{Page}";

    public string LinkId => $"{LinkIdPrefix}{NavId}";
}
