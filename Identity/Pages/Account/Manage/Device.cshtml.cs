namespace Identity.Pages.Account.Manage;

using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class DeviceModel : ConsentPageModelBase
{
    private readonly IDeviceFlowInteractionService _interaction;
    private readonly IEventService _events;

    public DeviceModel(
        IDeviceFlowInteractionService interaction,
        IEventService events)
    {
        _interaction = interaction;
        _events = events;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

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

    public async Task<IActionResult> OnPostAsync()
    {
        var userCode = Input.UserCode;
        ThrowIfNull(userCode);

        var request = await _interaction.GetAuthorizationContextAsync(userCode, HttpContext.RequestAborted);
        if (request == null)
        {
            return RedirectToPage("/Error");
        }

        ConsentResponse? grantedConsent = null;

        if (Input.Button == "no")
        {
            grantedConsent = new ConsentResponse { Error = InteractionError.AccessDenied };
            await _events.RaiseAsync(
                new ConsentDeniedEvent(
                    User.GetSubjectId(),
                    request.Client.ClientId,
                    request.ValidatedResources.RawScopeValues),
                HttpContext.RequestAborted);
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

                await _events.RaiseAsync(
                    new ConsentGrantedEvent(
                        User.GetSubjectId(),
                        request.Client.ClientId,
                        request.ValidatedResources.RawScopeValues,
                        grantedConsent.ScopesValuesConsented,
                        grantedConsent.RememberConsent),
                    HttpContext.RequestAborted);
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
            await _interaction.HandleRequestAsync(userCode, grantedConsent, HttpContext.RequestAborted);
            return RedirectToPage("/Account/Manage/DeviceSuccess");
        }

        if (!await SetViewModelAsync(userCode))
        {
            return RedirectToPage("/Error");
        }

        return Page();
    }

    private async Task<bool> SetViewModelAsync(string userCode)
    {
        var request = await _interaction.GetAuthorizationContextAsync(userCode, HttpContext.RequestAborted);
        if (request != null)
        {
            View = CreateConsentViewModel(request);
            return true;
        }

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

    public class InputModel
    {
        public string? UserCode { get; set; }

        public string Button { get; set; } = Empty;

        public IEnumerable<string> ScopesConsented { get; set; } = [];

        public bool RememberConsent { get; set; }

        public string? Description { get; set; }
    }
}