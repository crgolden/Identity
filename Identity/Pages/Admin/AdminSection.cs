namespace Identity.Pages.Admin;

public sealed record AdminSection(string Title, string Description, string Page)
{
    public const string CardIdPrefix = "admin-card-";

    public string CardId =>
        $"{CardIdPrefix}{Page.Split('/')[^2].ToLowerInvariant()}";

    public string Path => Page[..Page.LastIndexOf('/')];
}
