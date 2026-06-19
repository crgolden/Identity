namespace Identity.Pages.Admin.Clients.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Edits a client's scalar fields.</summary>
public class IndexModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets or sets the client being edited.</summary>
    [BindProperty]
    public Client Client { get; set; } = new();

    /// <summary>Loads the client for editing.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        return Page();
    }

    /// <summary>Saves the edited client.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        client.ClientId = Client.ClientId;
        client.ClientName = Client.ClientName;
        client.Description = Client.Description;
        client.ClientUri = Client.ClientUri;
        client.LogoUri = Client.LogoUri;
        client.ProtocolType = Client.ProtocolType;
        client.Enabled = Client.Enabled;
        client.RequireClientSecret = Client.RequireClientSecret;
        client.RequireConsent = Client.RequireConsent;
        client.AllowRememberConsent = Client.AllowRememberConsent;
        client.AlwaysIncludeUserClaimsInIdToken = Client.AlwaysIncludeUserClaimsInIdToken;
        client.RequirePkce = Client.RequirePkce;
        client.AllowPlainTextPkce = Client.AllowPlainTextPkce;
        client.RequireRequestObject = Client.RequireRequestObject;
        client.AllowAccessTokensViaBrowser = Client.AllowAccessTokensViaBrowser;
        client.RequireDPoP = Client.RequireDPoP;
        client.FrontChannelLogoutUri = Client.FrontChannelLogoutUri;
        client.FrontChannelLogoutSessionRequired = Client.FrontChannelLogoutSessionRequired;
        client.BackChannelLogoutUri = Client.BackChannelLogoutUri;
        client.BackChannelLogoutSessionRequired = Client.BackChannelLogoutSessionRequired;
        client.AllowOfflineAccess = Client.AllowOfflineAccess;
        client.IdentityTokenLifetime = Client.IdentityTokenLifetime;
        client.AllowedIdentityTokenSigningAlgorithms = Client.AllowedIdentityTokenSigningAlgorithms;
        client.AccessTokenLifetime = Client.AccessTokenLifetime;
        client.AuthorizationCodeLifetime = Client.AuthorizationCodeLifetime;
        client.ConsentLifetime = Client.ConsentLifetime;
        client.AbsoluteRefreshTokenLifetime = Client.AbsoluteRefreshTokenLifetime;
        client.SlidingRefreshTokenLifetime = Client.SlidingRefreshTokenLifetime;
        client.RefreshTokenUsage = Client.RefreshTokenUsage;
        client.UpdateAccessTokenClaimsOnRefresh = Client.UpdateAccessTokenClaimsOnRefresh;
        client.RefreshTokenExpiration = Client.RefreshTokenExpiration;
        client.AccessTokenType = Client.AccessTokenType;
        client.EnableLocalLogin = Client.EnableLocalLogin;
        client.IncludeJwtId = Client.IncludeJwtId;
        client.AlwaysSendClientClaims = Client.AlwaysSendClientClaims;
        client.ClientClaimsPrefix = Client.ClientClaimsPrefix;
        client.PairWiseSubjectSalt = Client.PairWiseSubjectSalt;
        client.InitiateLoginUri = Client.InitiateLoginUri;
        client.UserSsoLifetime = Client.UserSsoLifetime;
        client.UserCodeType = Client.UserCodeType;
        client.DeviceCodeLifetime = Client.DeviceCodeLifetime;
        client.CibaLifetime = Client.CibaLifetime;
        client.PollingInterval = Client.PollingInterval;
        client.CoordinateLifetimeWithUserSession = Client.CoordinateLifetimeWithUserSession;
        client.NonEditable = Client.NonEditable;
        client.PushedAuthorizationLifetime = Client.PushedAuthorizationLifetime;
        client.RequirePushedAuthorization = Client.RequirePushedAuthorization;
        client.Updated = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/Clients/Details/Index", new { id });
    }
}
