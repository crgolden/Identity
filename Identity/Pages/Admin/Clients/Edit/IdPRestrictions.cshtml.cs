namespace Identity.Pages.Admin.Clients.Edit;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;

public class IdPRestrictions : EditableClientResourcesBase<ClientIdPRestriction>
{
    internal const string DetailsPageName = "/Admin/Clients/Details/IdPRestrictions";

    public IdPRestrictions(IConfigurationDbContext context)
        : base(context)
    {
    }

    protected override Expression<Func<Client, List<ClientIdPRestriction>>> Collection => c => c.IdentityProviderRestrictions;

    protected override string DetailsPage => DetailsPageName;

    protected override int IdOf(ClientIdPRestriction resource) => resource.Id;

    protected override void CopyEditableFields(ClientIdPRestriction posted, ClientIdPRestriction existing) => existing.Provider = posted.Provider;

    protected override ClientIdPRestriction CreateForClient(ClientIdPRestriction posted, int clientId) =>
        new() { Provider = posted.Provider, ClientId = clientId };
}
