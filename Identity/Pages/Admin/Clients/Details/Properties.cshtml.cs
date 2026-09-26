namespace Identity.Pages.Admin.Clients.Details;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;

public class Properties : ClientResourcesBase<ClientProperty>
{
    public Properties(IConfigurationDbContext context)
        : base(context)
    {
    }

    protected override Expression<Func<Client, List<ClientProperty>>> Collection => c => c.Properties;
}
