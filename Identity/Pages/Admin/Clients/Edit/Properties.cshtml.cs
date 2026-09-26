namespace Identity.Pages.Admin.Clients.Edit;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;

public class Properties : EditableClientResourcesBase<ClientProperty>
{
    internal const string DetailsPageName = "/Admin/Clients/Details/Properties";

    public Properties(IConfigurationDbContext context)
        : base(context)
    {
    }

    protected override Expression<Func<Client, List<ClientProperty>>> Collection => c => c.Properties;

    protected override string DetailsPage => DetailsPageName;

    protected override int IdOf(ClientProperty resource) => resource.Id;

    protected override void CopyEditableFields(ClientProperty posted, ClientProperty existing)
    {
        existing.Key = posted.Key;
        existing.Value = posted.Value;
    }

    protected override ClientProperty CreateForClient(ClientProperty posted, int clientId) =>
        new() { Key = posted.Key, Value = posted.Value, ClientId = clientId };
}
