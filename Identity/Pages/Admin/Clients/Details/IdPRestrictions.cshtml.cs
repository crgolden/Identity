namespace Identity.Pages.Admin.Clients.Details;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;

public class IdPRestrictions : ClientResourcesBase<ClientIdPRestriction>
{
    public IdPRestrictions(IConfigurationDbContext context)
        : base(context)
    {
    }

    protected override Expression<Func<Client, List<ClientIdPRestriction>>> Collection => c => c.IdentityProviderRestrictions;
}
