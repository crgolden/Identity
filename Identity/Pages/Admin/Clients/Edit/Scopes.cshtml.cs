namespace Identity.Pages.Admin.Clients.Edit;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;

public class Scopes : EditableClientResourcesBase<ClientScope>
{
    internal const string DetailsPageName = "/Admin/Clients/Details/Scopes";

    public Scopes(IConfigurationDbContext context)
        : base(context)
    {
    }

    protected override Expression<Func<Client, List<ClientScope>>> Collection => c => c.AllowedScopes;

    protected override string DetailsPage => DetailsPageName;

    protected override int IdOf(ClientScope resource) => resource.Id;

    protected override void CopyEditableFields(ClientScope posted, ClientScope existing) => existing.Scope = posted.Scope;

    protected override ClientScope CreateForClient(ClientScope posted, int clientId) =>
        new() { Scope = posted.Scope, ClientId = clientId };
}
