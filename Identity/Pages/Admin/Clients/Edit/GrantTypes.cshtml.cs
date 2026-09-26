namespace Identity.Pages.Admin.Clients.Edit;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;

public class GrantTypes : EditableClientResourcesBase<ClientGrantType>
{
    internal const string DetailsPageName = "/Admin/Clients/Details/GrantTypes";

    public GrantTypes(IConfigurationDbContext context)
        : base(context)
    {
    }

    protected override Expression<Func<Client, List<ClientGrantType>>> Collection => c => c.AllowedGrantTypes;

    protected override string DetailsPage => DetailsPageName;

    protected override int IdOf(ClientGrantType resource) => resource.Id;

    protected override void CopyEditableFields(ClientGrantType posted, ClientGrantType existing) => existing.GrantType = posted.GrantType;

    protected override ClientGrantType CreateForClient(ClientGrantType posted, int clientId) =>
        new() { GrantType = posted.GrantType, ClientId = clientId };
}
