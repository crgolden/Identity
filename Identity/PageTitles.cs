namespace Identity;

internal static class PageTitles
{
    internal const string Application = "Identity";

    internal const string Home = "Home";

    internal static string Document(string? pageTitle) =>
        IsNullOrWhiteSpace(pageTitle) ? Application : $"{pageTitle} - {Application}";
}
