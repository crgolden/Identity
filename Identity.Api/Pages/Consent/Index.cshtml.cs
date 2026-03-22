namespace Identity.Pages.Consent;

using Duende.IdentityModel;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for the OAuth2 consent screen shown when a client requests authorization.</summary>
[Authorize]
[SecurityHeaders]
public class IndexModel : PageModel
{
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IEventService _events;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IIdentityServerInteractionService interaction,
        IEventService events,
        ILogger<IndexModel> logger)
    {
        _interaction = interaction;
        _events = events;
        _logger = logger;
    }

    /// <summary>Gets or sets the view model populated on GET.</summary>
    public ViewModel View { get; set; } = new ViewModel();

    /// <summary>Gets or sets the bound input model from the consent form POST.</summary>
    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    /// <summary>Handles the GET request to display the consent form.</summary>
    /// <param name="returnUrl">The authorization return URL identifying the pending request.</param>
    /// <returns>A task resolving to the page result, or a redirect to the error page on invalid state.</returns>
    public async Task<IActionResult> OnGetAsync(string? returnUrl)
    {
        if (!await SetViewModelAsync(returnUrl))
        {
            return RedirectToPage("/Error");
        }

        Input = new InputModel { ReturnUrl = returnUrl };
        return Page();
    }

    /// <summary>Handles the POST request when the user submits the consent form.</summary>
    /// <returns>A task resolving to a redirect after processing the consent decision.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        var request = await _interaction.GetAuthorizationContextAsync(Input.ReturnUrl);
        if (request == null)
        {
            return RedirectToPage("/Error");
        }

        ConsentResponse? grantedConsent = null;

        if (Input.Button == "no")
        {
            grantedConsent = new ConsentResponse { Error = AuthorizationError.AccessDenied };
            await _events.RaiseAsync(new ConsentDeniedEvent(
                User.GetSubjectId(),
                request.Client.ClientId,
                request.ValidatedResources.RawScopeValues));
            Telemetry.Metrics.ConsentDenied(
                request.Client.ClientId,
                request.ValidatedResources.ParsedScopes.Select(s => s.ParsedName));
        }
        else if (Input.Button == "yes")
        {
            if (Input.ScopesConsented.Any())
            {
                var scopes = Input.ScopesConsented;
                if (!ConsentOptions.EnableOfflineAccess)
                {
                    scopes = scopes.Where(x =>
                        x != Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess);
                }

                grantedConsent = new ConsentResponse
                {
                    RememberConsent = Input.RememberConsent,
                    ScopesValuesConsented = scopes.ToArray(),
                    Description = Input.Description,
                };

                await _events.RaiseAsync(new ConsentGrantedEvent(
                    User.GetSubjectId(),
                    request.Client.ClientId,
                    request.ValidatedResources.RawScopeValues,
                    grantedConsent.ScopesValuesConsented,
                    grantedConsent.RememberConsent));
                Telemetry.Metrics.ConsentGranted(
                    request.Client.ClientId,
                    grantedConsent.ScopesValuesConsented,
                    grantedConsent.RememberConsent);
                var denied = request.ValidatedResources.ParsedScopes
                    .Select(s => s.ParsedName)
                    .Except(grantedConsent.ScopesValuesConsented);
                Telemetry.Metrics.ConsentDenied(request.Client.ClientId, denied);
            }
            else
            {
                ModelState.AddModelError(Empty, ConsentOptions.MustChooseOneErrorMessage);
            }
        }
        else
        {
            ModelState.AddModelError(Empty, ConsentOptions.InvalidSelectionErrorMessage);
        }

        if (grantedConsent != null)
        {
            ThrowIfNull(Input.ReturnUrl);
            await _interaction.GrantConsentAsync(request, grantedConsent, User.GetSubjectId());
            return Redirect(Input.ReturnUrl);
        }

        if (!await SetViewModelAsync(Input.ReturnUrl))
        {
            return RedirectToPage("/Error");
        }

        return Page();
    }

    private async Task<bool> SetViewModelAsync(string? returnUrl)
    {
        if (IsNullOrWhiteSpace(returnUrl))
        {
            _logger.LogWarning("Consent page loaded with no return URL.");
            return false;
        }

        var request = await _interaction.GetAuthorizationContextAsync(returnUrl);
        if (request != null)
        {
            View = CreateConsentViewModel(request);
            return true;
        }

        _logger.LogWarning("No consent request matching return URL: {ReturnUrl}", returnUrl);
        return false;
    }

    private ViewModel CreateConsentViewModel(AuthorizationRequest request)
    {
        var vm = new ViewModel
        {
            ClientName = request.Client.ClientName ?? request.Client.ClientId,
            ClientUrl = request.Client.ClientUri,
            ClientLogoUrl = request.Client.LogoUri,
            AllowRememberConsent = request.Client.AllowRememberConsent,
        };

        vm.IdentityScopes = request.ValidatedResources.Resources.IdentityResources
            .Select(x => CreateScopeViewModel(x, Input == null || Input.ScopesConsented.Contains(x.Name)))
            .ToArray();

        var resourceIndicators =
            request.Parameters.GetValues(OidcConstants.AuthorizeRequest.Resource)
            ?? Enumerable.Empty<string>();
        var apiResources = request.ValidatedResources.Resources.ApiResources
            .Where(x => resourceIndicators.Contains(x.Name));

        var apiScopes = new List<ScopeViewModel>();
        foreach (var parsedScope in request.ValidatedResources.ParsedScopes)
        {
            var apiScope = request.ValidatedResources.Resources.FindApiScope(parsedScope.ParsedName);
            if (apiScope != null)
            {
                var scopeVm = CreateScopeViewModel(
                    parsedScope,
                    apiScope,
                    Input == null || Input.ScopesConsented.Contains(parsedScope.RawValue));
                scopeVm.Resources = apiResources
                    .Where(x => x.Scopes.Contains(parsedScope.ParsedName))
                    .Select(x => new ResourceViewModel
                    {
                        Name = x.Name,
                        DisplayName = x.DisplayName ?? x.Name,
                    })
                    .ToArray();
                apiScopes.Add(scopeVm);
            }
        }

        if (ConsentOptions.EnableOfflineAccess && request.ValidatedResources.Resources.OfflineAccess)
        {
            apiScopes.Add(CreateOfflineAccessScope(
                Input == null || Input.ScopesConsented.Contains(
                    Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess)));
        }

        vm.ApiScopes = apiScopes;
        return vm;
    }

    private ScopeViewModel CreateScopeViewModel(IdentityResource identity, bool check) =>
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

    private ScopeViewModel CreateScopeViewModel(
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

    private ScopeViewModel CreateOfflineAccessScope(bool check) =>
        new()
        {
            Value = Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess,
            DisplayName = ConsentOptions.OfflineAccessDisplayName,
            Description = ConsentOptions.OfflineAccessDescription,
            Emphasize = true,
            Checked = check,
        };

    /// <summary>View model for the consent page.</summary>
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

    /// <summary>Input model bound from the consent form POST.</summary>
    public class InputModel
    {
        /// <summary>Gets or sets the button clicked by the user ("yes" or "no").</summary>
        public string Button { get; set; } = Empty;

        /// <summary>Gets or sets the scope values the user selected.</summary>
        public IEnumerable<string> ScopesConsented { get; set; } = [];

        /// <summary>Gets or sets a value indicating whether the user chose to remember their consent decision.</summary>
        public bool RememberConsent { get; set; }

        /// <summary>Gets or sets the authorization return URL.</summary>
        public string? ReturnUrl { get; set; }

        /// <summary>Gets or sets an optional description or device name provided by the user.</summary>
        public string? Description { get; set; }
    }

    /// <summary>View model for a single scope item on the consent page.</summary>
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
