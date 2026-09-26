namespace Identity.Pages.Admin.Clients.Edit;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;

public class Secrets : EditableClientResourcesBase<ClientSecret>
{
    internal const string DetailsPageName = "/Admin/Clients/Details/Secrets";

    public Secrets(IConfigurationDbContext context)
        : base(context)
    {
    }

    protected override Expression<Func<Client, List<ClientSecret>>> Collection => c => c.ClientSecrets;

    protected override string DetailsPage => DetailsPageName;

    protected override int IdOf(ClientSecret resource) => resource.Id;

    protected override void CopyEditableFields(ClientSecret posted, ClientSecret existing)
    {
        existing.Description = posted.Description;
        existing.Type = posted.Type;
        existing.Expiration = posted.Expiration;
    }

    protected override ClientSecret CreateForClient(ClientSecret posted, int clientId) => new()
    {
        Description = posted.Description,
        Value = posted.Value,
        Type = posted.Type,
        Expiration = posted.Expiration,
        ClientId = clientId,
    };

    protected override ClientSecret NewResource() => new ClientSecret { Type = "SharedSecret" };
}
