namespace Identity.Pages.Admin.Clients.Details;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;

public class GrantTypes : ClientResourcesBase<ClientGrantType>
{
    public GrantTypes(IConfigurationDbContext context)
        : base(context)
    {
    }

    protected override Expression<Func<Client, List<ClientGrantType>>> Collection => c => c.AllowedGrantTypes;
}
