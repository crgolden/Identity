namespace Identity.Pages.Device;

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using Filters;
using Consent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

/// <summary>Page model for the device authorization flow consent screen.</summary>
[Authorize]
[SecurityHeaders]
public class IndexModel : PageModel
{
    private readonly IDeviceFlowInteractionService _interaction;
    private readonly IEventService _events;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IDeviceFlowInteractionService interaction,
        IEventService events,
        IOptions<IdentityServerOptions> options,
        ILogger<IndexModel> logger)
    {
        _interaction = interaction;
        _events = events;
        _logger = logger;
    }

    /// <summary>Gets or sets the view model for the consent portion of the device flow.</summary>
    public ViewModel View { get; set; } = new ViewModel();

    /// <summary>Gets or sets the bound input model from the device flow consent form.</summary>
    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    /// <summary>Handles the GET request to display the device authorization consent or user code entry form.</summary>
    /// <param name="userCode">The user code from the device, if already known.</param>
    /// <returns>A task resolving to the page result.</returns>
    public async Task<IActionResult> OnGetAsync(string? userCode)
    {
        if (IsNullOrWhiteSpace(userCode))
        {
            return Page();
        }

        if (!await SetViewModelAsync(userCode))
        {
            ModelState.AddModelError(Empty, "Invalid user code.");
            return Page();
        }

        Input = new InputModel { UserCode = userCode };
        return Page();
    }

    /// <summary>Handles the POST request when the user submits the device flow consent form.</summary>
    /// <returns>A task resolving to a redirect to the success page or back to the consent form.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        var userCode = Input.UserCode;
        ThrowIfNull(userCode);

        var request = await _interaction.GetAuthorizationContextAsync(userCode);
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
            await _interaction.HandleRequestAsync(userCode, grantedConsent);
            return RedirectToPage("/Device/Success");
        }

        if (!await SetViewModelAsync(userCode))
        {
            return RedirectToPage("/Error");
        }

        return Page();
    }

    private async Task<bool> SetViewModelAsync(string userCode)
    {
        var request = await _interaction.GetAuthorizationContextAsync(userCode);
        if (request != null)
        {
            View = CreateConsentViewModel(request);
            return true;
        }

        _logger.LogWarning("No device flow authorization context found for user code.");
        View = new ViewModel();
        return false;
    }

    private ViewModel CreateConsentViewModel(DeviceFlowAuthorizationRequest request)
    {
        var vm = new ViewModel
        {
            ClientName = request.Client.ClientName ?? request.Client.ClientId,
            ClientUrl = request.Client.ClientUri,
            ClientLogoUrl = request.Client.LogoUri,
            AllowRememberConsent = request.Client.AllowRememberConsent,
        };

        vm.IdentityScopes = request.ValidatedResources.Resources.IdentityResources
            .Select(x => CreateScopeViewModel(x, Input.ScopesConsented.Contains(x.Name)))
            .ToArray();

        var apiScopes = new List<ScopeViewModel>();
        foreach (var parsedScope in request.ValidatedResources.ParsedScopes)
        {
            var apiScope = request.ValidatedResources.Resources.FindApiScope(parsedScope.ParsedName);
            if (apiScope != null)
            {
                apiScopes.Add(CreateScopeViewModel(
                    parsedScope,
                    apiScope,
                    Input == null || Input.ScopesConsented.Contains(parsedScope.RawValue)));
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
        bool check) =>
        new()
        {
            Value = parsedScopeValue.RawValue,
            DisplayName = apiScope.DisplayName ?? apiScope.Name,
            Description = apiScope.Description,
            Emphasize = apiScope.Emphasize,
            Required = apiScope.Required,
            Checked = check || apiScope.Required,
        };

    private ScopeViewModel CreateOfflineAccessScope(bool check) =>
        new()
        {
            Value = Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess,
            DisplayName = ConsentOptions.OfflineAccessDisplayName,
            Description = ConsentOptions.OfflineAccessDescription,
            Emphasize = true,
            Checked = check,
        };

    /// <summary>View model for the device flow consent page.</summary>
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

    /// <summary>Input model bound from the device flow consent form POST.</summary>
    public class InputModel
    {
        /// <summary>Gets or sets the user code identifying this device authorization request.</summary>
        public string? UserCode { get; set; }

        /// <summary>Gets or sets the button clicked by the user ("yes" or "no").</summary>
        public string Button { get; set; } = Empty;

        /// <summary>Gets or sets the scope values the user selected.</summary>
        public IEnumerable<string> ScopesConsented { get; set; } = [];

        /// <summary>Gets or sets a value indicating whether the user chose to remember their consent decision.</summary>
        public bool RememberConsent { get; set; }

        /// <summary>Gets or sets an optional description or device name provided by the user.</summary>
        public string? Description { get; set; }
    }

    /// <summary>View model for a single scope item on the device flow consent page.</summary>
    public class ScopeViewModel
    {
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
    }
}
