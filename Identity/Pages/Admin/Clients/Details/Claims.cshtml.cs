namespace Identity.Pages.Admin.Clients.Details;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;

public class Claims : ClientResourcesBase<ClientClaim>
{
    public Claims(IConfigurationDbContext context)
        : base(context)
    {
    }

    protected override Expression<Func<Client, List<ClientClaim>>> Collection => c => c.Claims;
}
