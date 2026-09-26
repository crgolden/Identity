namespace Identity.Tests.E2E.Admin;

internal sealed record ClientCollection(string Page)
{
    public string IdPrefix => Page.ToLowerInvariant()[..^1];
}
