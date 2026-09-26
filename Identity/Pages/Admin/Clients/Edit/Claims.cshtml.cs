namespace Identity.Pages.Admin.Clients.Edit;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;

public class Claims : EditableClientResourcesBase<ClientClaim>
{
    internal const string DetailsPageName = "/Admin/Clients/Details/Claims";

    public Claims(IConfigurationDbContext context)
        : base(context)
    {
    }

    protected override Expression<Func<Client, List<ClientClaim>>> Collection => c => c.Claims;

    protected override string DetailsPage => DetailsPageName;

    protected override int IdOf(ClientClaim resource) => resource.Id;

    protected override void CopyEditableFields(ClientClaim posted, ClientClaim existing)
    {
        existing.Type = posted.Type;
        existing.Value = posted.Value;
    }

    protected override ClientClaim CreateForClient(ClientClaim posted, int clientId) =>
        new() { Type = posted.Type, Value = posted.Value, ClientId = clientId };
}
