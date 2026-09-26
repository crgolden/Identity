namespace Identity.Pages.Admin.Clients.Details;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;

public class RedirectUris : ClientResourcesBase<ClientRedirectUri>
{
    public RedirectUris(IConfigurationDbContext context)
        : base(context)
    {
    }

    protected override Expression<Func<Client, List<ClientRedirectUri>>> Collection => c => c.RedirectUris;
}
