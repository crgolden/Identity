namespace Identity.Pages.Account.Manage;

using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Authorize]
public class GrantsModel : PageModel
{
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IClientStore _clients;
    private readonly IResourceStore _resources;
    private readonly IEventService _events;

    public GrantsModel(
        IIdentityServerInteractionService interaction,
        IClientStore clients,
        IResourceStore resources,
        IEventService events)
    {
        ThrowIfNull(interaction);
        ThrowIfNull(clients);
        ThrowIfNull(resources);
        ThrowIfNull(events);
        _interaction = interaction;
        _clients = clients;
        _resources = resources;
        _events = events;
    }

    public ViewModel View { get; set; } = new ViewModel();

    [BindProperty]
    public string? ClientId { get; set; }

    public async Task OnGetAsync()
    {
        var grants = await _interaction.GetAllUserGrantsAsync(HttpContext.RequestAborted);
        var list = new List<GrantViewModel>();

        foreach (var grant in grants)
        {
            var client = await _clients.FindClientByIdAsync(grant.ClientId, HttpContext.RequestAborted);
            if (client != null)
            {
                var grantResources = await _resources.FindResourcesByScopeAsync(grant.Scopes, HttpContext.RequestAborted);
                list.Add(new GrantViewModel
                {
                    ClientId = client.ClientId,
                    ClientName = client.ClientName ?? client.ClientId,
                    ClientLogoUrl = client.LogoUri,
                    ClientUrl = client.ClientUri,
                    Description = grant.Description,
                    Created = grant.CreationTime,
                    Expires = grant.Expiration,
                    IdentityGrantNames = grantResources.IdentityResources
                        .Select(x => x.DisplayName ?? x.Name)
                        .ToArray(),
                    ApiGrantNames = grantResources.ApiScopes
                        .Select(x => x.DisplayName ?? x.Name)
                        .ToArray(),
                });
            }
        }

        View = new ViewModel { Grants = list };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _interaction.RevokeUserConsentAsync(ClientId, HttpContext.RequestAborted);
        await _events.RaiseAsync(new GrantsRevokedEvent(User.GetSubjectId(), ClientId), HttpContext.RequestAborted);
        Telemetry.Metrics.GrantsRevoked(ClientId);
        using var activity = Telemetry.StartActivity("identity.grants.revoke");
        activity?.SetTag("client_id", ClientId);
        return RedirectToPage("/Account/Manage/Grants");
    }

    public class ViewModel
    {
        public IEnumerable<GrantViewModel> Grants { get; set; } = [];
    }

    public class GrantViewModel
    {
        public string ClientId { get; set; } = Empty;

        public string ClientName { get; set; } = Empty;

        public string? ClientLogoUrl { get; set; }

        public string? ClientUrl { get; set; }

        public string? Description { get; set; }

        public DateTime Created { get; set; }

        public DateTime? Expires { get; set; }

        public IEnumerable<string> IdentityGrantNames { get; set; } = [];

        public IEnumerable<string> ApiGrantNames { get; set; } = [];
    }
}