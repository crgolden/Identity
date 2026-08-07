namespace Identity.Pages.Account.Manage;

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;

public abstract class ConsentPageModelBase : PageModel
{
    public ViewModel View { get; set; } = new ViewModel();

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

    protected static ScopeViewModel CreateOfflineAccessScope(bool check) =>
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
        public string ClientName { get; set; } = Empty;

        public string? ClientUrl { get; set; }

        public string? ClientLogoUrl { get; set; }

        public bool AllowRememberConsent { get; set; }

        public IEnumerable<ScopeViewModel> IdentityScopes { get; set; } = [];

        public IEnumerable<ScopeViewModel> ApiScopes { get; set; } = [];
    }

    public class ScopeViewModel
    {
        public string Name { get; set; } = Empty;

        public string Value { get; set; } = Empty;

        public string DisplayName { get; set; } = Empty;

        public string? Description { get; set; }

        public bool Emphasize { get; set; }

        public bool Required { get; set; }

        public bool Checked { get; set; }

        public IEnumerable<ResourceViewModel> Resources { get; set; } = [];
    }

    public class ResourceViewModel
    {
        public string Name { get; set; } = Empty;

        public string DisplayName { get; set; } = Empty;
    }
}