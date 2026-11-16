namespace Identity.Pages;

internal static class UriValue
{
    internal static Uri? From(string? value) =>
        Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
}
