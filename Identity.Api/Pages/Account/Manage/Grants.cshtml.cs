namespace Identity.Pages.Account.Manage;

using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for viewing and revoking previously granted client application permissions.</summary>
[Authorize]
[SecurityHeaders]
public class GrantsModel : PageModel
{
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IClientStore _clients;
    private readonly IResourceStore _resources;
    private readonly IEventService _events;

    /// <summary>Initializes a new instance of <see cref="GrantsModel"/>.</summary>
    /// <param name="interaction">The IdentityServer interaction service.</param>
    /// <param name="clients">The IdentityServer client store.</param>
    /// <param name="resources">The IdentityServer resource store.</param>
    /// <param name="events">The IdentityServer event service.</param>
    public GrantsModel(
        IIdentityServerInteractionService interaction,
        IClientStore clients,
        IResourceStore resources,
        IEventService events)
    {
        _interaction = interaction;
        _clients = clients;
        _resources = resources;
        _events = events;
    }

    /// <summary>Gets or sets the view model listing all current grants.</summary>
    public ViewModel View { get; set; } = new ViewModel();

    /// <summary>Gets or sets the client ID to revoke, bound from the revoke form.</summary>
    [BindProperty]
    public string? ClientId { get; set; }

    /// <summary>Handles the GET request to load the user's current grants.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task OnGetAsync()
    {
        var grants = await _interaction.GetAllUserGrantsAsync();
        var list = new List<GrantViewModel>();

        foreach (var grant in grants)
        {
            var client = await _clients.FindClientByIdAsync(grant.ClientId);
            if (client != null)
            {
                var grantResources = await _resources.FindResourcesByScopeAsync(grant.Scopes);
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

    /// <summary>Handles the POST request to revoke a client's grants.</summary>
    /// <returns>A task resolving to a redirect back to this page after revoking.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        await _interaction.RevokeUserConsentAsync(ClientId);
        await _events.RaiseAsync(new GrantsRevokedEvent(User.GetSubjectId(), ClientId));
        Telemetry.Metrics.GrantsRevoked(ClientId);
        return RedirectToPage("/Account/Manage/Grants");
    }

    /// <summary>View model containing the list of grants for the current user.</summary>
    public class ViewModel
    {
        /// <summary>Gets or sets the list of grants to display.</summary>
        public IEnumerable<GrantViewModel> Grants { get; set; } = [];
    }

    /// <summary>View model for a single client grant entry.</summary>
    public class GrantViewModel
    {
        /// <summary>Gets or sets the client ID.</summary>
        public string ClientId { get; set; } = Empty;

        /// <summary>Gets or sets the display name of the client.</summary>
        public string ClientName { get; set; } = Empty;

        /// <summary>Gets or sets the URI of the client's logo.</summary>
        public string? ClientLogoUrl { get; set; }

        /// <summary>Gets or sets the URI of the client's website.</summary>
        public string? ClientUrl { get; set; }

        /// <summary>Gets or sets the optional description provided by the user when granting consent.</summary>
        public string? Description { get; set; }

        /// <summary>Gets or sets the date the grant was created.</summary>
        public DateTime Created { get; set; }

        /// <summary>Gets or sets the optional expiry date of the grant.</summary>
        public DateTime? Expires { get; set; }

        /// <summary>Gets or sets the display names of the granted identity scopes.</summary>
        public IEnumerable<string> IdentityGrantNames { get; set; } = [];

        /// <summary>Gets or sets the display names of the granted API scopes.</summary>
        public IEnumerable<string> ApiGrantNames { get; set; } = [];
    }
}
