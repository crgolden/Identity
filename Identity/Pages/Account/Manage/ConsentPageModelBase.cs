namespace Identity.Pages.Account.Manage;

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

public abstract class ConsentPageModelBase : PageModel
{
    internal const string MustChooseOneErrorMessage = "You must pick at least one permission.";

    internal const string InvalidSelectionErrorMessage = "Invalid selection.";

    protected ConsentPageModelBase(IOptions<ConsentOptions> consentOptions) => ConsentOptions = consentOptions.Value;

    public ViewModel View { get; set; } = new ViewModel();

    protected static Func<string, bool> EveryScope { get; } = static _ => true;

    protected ConsentOptions ConsentOptions { get; }

    protected static ScopeViewModel CreateScopeViewModel(IdentityResource identity, bool check) =>
        new()
        {
            Name = identity.Name,
            Value = identity.Name,
            DisplayName = identity.DisplayName ?? identity.Name,
            Description = identity.Description,
            Emphasize = identity.Emphasize,
            Required = identity.Required,
            Checked = check || identity.Required,
        };

    protected static ScopeViewModel CreateScopeViewModel(
        ParsedScopeValue parsedScopeValue,
        ApiScope apiScope,
        bool check)
    {
        var displayName = apiScope.DisplayName ?? apiScope.Name;
        if (!IsNullOrWhiteSpace(parsedScopeValue.ParsedParameter))
        {
            displayName += ":" + parsedScopeValue.ParsedParameter;
        }

        return new ScopeViewModel
        {
            Name = parsedScopeValue.ParsedName,
            Value = parsedScopeValue.RawValue,
            DisplayName = displayName,
            Description = apiScope.Description,
            Emphasize = apiScope.Emphasize,
            Required = apiScope.Required,
            Checked = check || apiScope.Required,
        };
    }

    protected ScopeViewModel CreateOfflineAccessScope(bool check) =>
        new()
        {
            Value = Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess,
            DisplayName = ConsentOptions.OfflineAccessDisplayName,
            Description = ConsentOptions.OfflineAccessDescription,
            Emphasize = true,
            Checked = check,
        };

    public class ViewModel
    {
        public string? ClientName { get; set; }

        public string? ClientUrl { get; set; }

        public string? ClientLogoUrl { get; set; }

        public bool AllowRememberConsent { get; set; }

        public IEnumerable<ScopeViewModel> IdentityScopes { get; set; } = [];

        public IEnumerable<ScopeViewModel> ApiScopes { get; set; } = [];
    }

    public class ScopeViewModel
    {
        public string? Name { get; set; }

        public required string Value { get; set; }

        public required string DisplayName { get; set; }

        public string? Description { get; set; }

        public bool Emphasize { get; set; }

        public bool Required { get; set; }

        public bool Checked { get; set; }

        public IEnumerable<ResourceViewModel> Resources { get; set; } = [];
    }

    public class ResourceViewModel
    {
        public required string Name { get; set; }

        public required string DisplayName { get; set; }
    }
}
