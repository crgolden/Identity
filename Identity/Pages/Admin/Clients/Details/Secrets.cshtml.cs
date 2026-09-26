namespace Identity.Pages.Admin.Clients.Details;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;

public class Secrets : ClientResourcesBase<ClientSecret>
{
    public Secrets(IConfigurationDbContext context)
        : base(context)
    {
    }

    protected override Expression<Func<Client, List<ClientSecret>>> Collection => c => c.ClientSecrets;
}
