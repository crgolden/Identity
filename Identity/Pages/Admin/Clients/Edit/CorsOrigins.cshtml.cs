namespace Identity.Pages.Admin.Clients.Edit;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;

public class CorsOrigins : EditableClientResourcesBase<ClientCorsOrigin>
{
    internal const string DetailsPageName = "/Admin/Clients/Details/CorsOrigins";

    public CorsOrigins(IConfigurationDbContext context)
        : base(context)
    {
    }

    protected override Expression<Func<Client, List<ClientCorsOrigin>>> Collection => c => c.AllowedCorsOrigins;

    protected override string DetailsPage => DetailsPageName;

    protected override int IdOf(ClientCorsOrigin resource) => resource.Id;

    protected override void CopyEditableFields(ClientCorsOrigin posted, ClientCorsOrigin existing) => existing.Origin = posted.Origin;

    protected override ClientCorsOrigin CreateForClient(ClientCorsOrigin posted, int clientId) =>
        new() { Origin = posted.Origin, ClientId = clientId };
}
