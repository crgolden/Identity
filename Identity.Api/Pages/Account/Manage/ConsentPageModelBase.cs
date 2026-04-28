namespace Identity.Pages.Account.Manage;

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Abstract base page model shared by the OAuth2 consent and device authorization consent pages.</summary>
public abstract class ConsentPageModelBase : PageModel
{
    /// <summary>Gets or sets the view model for the consent form.</summary>
    public ViewModel View { get; set; } = new ViewModel();

    /// <summary>Creates a scope view model from an identity resource.</summary>
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

    /// <summary>Creates a scope view model from a parsed API scope value.</summary>
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

    /// <summary>Creates a scope view model for the offline access scope.</summary>
    protected static ScopeViewModel CreateOfflineAccessScope(bool check) =>
        new()
        {
            Value = Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess,
            DisplayName = ConsentOptions.OfflineAccessDisplayName,
            Description = ConsentOptions.OfflineAccessDescription,
            Emphasize = true,
            Checked = check,
        };

    /// <summary>View model shared by consent and device authorization pages.</summary>
    public class ViewModel
    {
        /// <summary>Gets or sets the display name of the requesting client.</summary>
        public string ClientName { get; set; } = Empty;

        /// <summary>Gets or sets the URI of the requesting client's website.</summary>
        public string? ClientUrl { get; set; }

        /// <summary>Gets or sets the URI of the requesting client's logo.</summary>
        public string? ClientLogoUrl { get; set; }

        /// <summary>Gets or sets a value indicating whether the client allows the user to remember their consent decision.</summary>
        public bool AllowRememberConsent { get; set; }

        /// <summary>Gets or sets the identity scopes to display.</summary>
        public IEnumerable<ScopeViewModel> IdentityScopes { get; set; } = [];

        /// <summary>Gets or sets the API scopes to display.</summary>
        public IEnumerable<ScopeViewModel> ApiScopes { get; set; } = [];
    }

    /// <summary>View model for a single scope item on a consent page.</summary>
    public class ScopeViewModel
    {
        /// <summary>Gets or sets the scope name (used for lookups).</summary>
        public string Name { get; set; } = Empty;

        /// <summary>Gets or sets the raw scope value sent in the consent response.</summary>
        public string Value { get; set; } = Empty;

        /// <summary>Gets or sets the human-readable display name.</summary>
        public string DisplayName { get; set; } = Empty;

        /// <summary>Gets or sets the optional description.</summary>
        public string? Description { get; set; }

        /// <summary>Gets or sets a value indicating whether this scope should be visually emphasized.</summary>
        public bool Emphasize { get; set; }

        /// <summary>Gets or sets a value indicating whether this scope is required and cannot be unchecked.</summary>
        public bool Required { get; set; }

        /// <summary>Gets or sets a value indicating whether the scope checkbox is checked.</summary>
        public bool Checked { get; set; }

        /// <summary>Gets or sets the API resources associated with this scope.</summary>
        public IEnumerable<ResourceViewModel> Resources { get; set; } = [];
    }

    /// <summary>View model for an API resource associated with a scope.</summary>
    public class ResourceViewModel
    {
        /// <summary>Gets or sets the resource name.</summary>
        public string Name { get; set; } = Empty;

        /// <summary>Gets or sets the human-readable display name.</summary>
        public string DisplayName { get; set; } = Empty;
    }
}
