namespace Identity.Pages.Admin.Clients.Details;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;

public class PostLogoutRedirectUris : ClientResourcesBase<ClientPostLogoutRedirectUri>
{
    public PostLogoutRedirectUris(IConfigurationDbContext context)
        : base(context)
    {
    }

    protected override Expression<Func<Client, List<ClientPostLogoutRedirectUri>>> Collection => c => c.PostLogoutRedirectUris;
}
