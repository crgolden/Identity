namespace Identity.Pages.Account.Manage;

using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[Authorize]
public class Device : ConsentPageModelBase
{
    internal const string DeviceSuccessPagePath = "/Account/Manage/DeviceSuccess";

    internal const string InvalidUserCodeMessage = "Invalid user code.";

    private readonly IDeviceFlowInteractionService _interaction;
    private readonly IEventService _events;
    private readonly Telemetry _telemetry;

    public Device(
        IDeviceFlowInteractionService interaction,
        IEventService events,
        IOptions<ConsentOptions> consentOptions,
        Telemetry telemetry)
        : base(consentOptions)
    {
        _interaction = interaction;
        _events = events;
        _telemetry = telemetry;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    public async Task<IActionResult> OnGetAsync(string? userCode)
    {
        if (IsNullOrWhiteSpace(userCode))
        {
            return Page();
        }

        if (!await SetViewModelAsync(userCode, EveryScope))
        {
            ModelState.AddModelError(Empty, InvalidUserCodeMessage);
            return Page();
        }

        Input = new InputModel { UserCode = userCode };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userCode = Input.UserCode;
        ThrowIfNull(userCode);

        var request = await _interaction.GetAuthorizationContextAsync(userCode, HttpContext.RequestAborted);
        if (request == null)
        {
            return RedirectToPage(PageRoutes.Error);
        }

        ConsentResponse? grantedConsent = null;

        if (string.Equals(Input.Button, Consent.DenyButtonValue, StringComparison.Ordinal))
        {
            grantedConsent = new ConsentResponse { Error = InteractionError.AccessDenied };
            await _events.RaiseAsync(
                new ConsentDeniedEvent(
                    User.GetSubjectId(),
                    request.Client.ClientId,
                    request.ValidatedResources.RawScopeValues),
                HttpContext.RequestAborted);
            _telemetry.ConsentDenied(
                request.Client.ClientId,
                request.ValidatedResources.ParsedScopes.Select(s => s.ParsedName));
        }
        else if (string.Equals(Input.Button, Consent.GrantButtonValue, StringComparison.Ordinal))
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

                await _events.RaiseAsync(
                    new ConsentGrantedEvent(
                        User.GetSubjectId(),
                        request.Client.ClientId,
                        request.ValidatedResources.RawScopeValues,
                        grantedConsent.ScopesValuesConsented,
                        grantedConsent.RememberConsent),
                    HttpContext.RequestAborted);
                _telemetry.ConsentGranted(
                    request.Client.ClientId,
                    grantedConsent.ScopesValuesConsented,
                    grantedConsent.RememberConsent);
                var denied = request.ValidatedResources.ParsedScopes
                    .Select(s => s.ParsedName)
                    .Except(grantedConsent.ScopesValuesConsented, StringComparer.Ordinal);
                _telemetry.ConsentDenied(request.Client.ClientId, denied);
            }
            else
            {
                ModelState.AddModelError(Empty, MustChooseOneErrorMessage);
            }
        }
        else
        {
            ModelState.AddModelError(Empty, InvalidSelectionErrorMessage);
        }

        if (grantedConsent != null)
        {
            await _interaction.HandleRequestAsync(userCode, grantedConsent, HttpContext.RequestAborted);
            return RedirectToPage(DeviceSuccessPagePath);
        }

        if (!await SetViewModelAsync(userCode, Input.ScopesConsented.Contains))
        {
            return RedirectToPage(PageRoutes.Error);
        }

        return Page();
    }

    private ViewModel CreateConsentViewModel(DeviceFlowAuthorizationRequest request, Func<string, bool> isConsented)
    {
        var vm = new ViewModel
        {
            ClientName = request.Client.ClientName ?? request.Client.ClientId,
            ClientUrl = request.Client.ClientUri,
            ClientLogoUrl = request.Client.LogoUri,
            AllowRememberConsent = request.Client.AllowRememberConsent,
        };

        vm.IdentityScopes = request.ValidatedResources.Resources.IdentityResources
            .Select(x => CreateScopeViewModel(x, isConsented(x.Name)))
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
                    isConsented(parsedScope.RawValue)));
            }
        }

        if (ConsentOptions.EnableOfflineAccess && request.ValidatedResources.Resources.OfflineAccess)
        {
            apiScopes.Add(CreateOfflineAccessScope(
                isConsented(Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess)));
        }

        vm.ApiScopes = apiScopes;
        return vm;
    }

    private async Task<bool> SetViewModelAsync(string userCode, Func<string, bool> isConsented)
    {
        var request = await _interaction.GetAuthorizationContextAsync(userCode, HttpContext.RequestAborted);
        if (request != null)
        {
            View = CreateConsentViewModel(request, isConsented);
            return true;
        }

        View = new ViewModel();
        return false;
    }

    public class InputModel
    {
        public string? UserCode { get; set; }

        public string? Button { get; set; }

        public IEnumerable<string> ScopesConsented { get; set; } = [];

        public bool RememberConsent { get; set; }

        public string? Description { get; set; }
    }
}
