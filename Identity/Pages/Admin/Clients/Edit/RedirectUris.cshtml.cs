namespace Identity.Pages.Admin.Clients.Edit;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;

public class RedirectUris : EditableClientResourcesBase<ClientRedirectUri>
{
    internal const string DetailsPageName = "/Admin/Clients/Details/RedirectUris";

    public RedirectUris(IConfigurationDbContext context)
        : base(context)
    {
    }

    protected override Expression<Func<Client, List<ClientRedirectUri>>> Collection => c => c.RedirectUris;

    protected override string DetailsPage => DetailsPageName;

    protected override int IdOf(ClientRedirectUri resource) => resource.Id;

    protected override void CopyEditableFields(ClientRedirectUri posted, ClientRedirectUri existing) => existing.RedirectUri = posted.RedirectUri;

    protected override ClientRedirectUri CreateForClient(ClientRedirectUri posted, int clientId) =>
        new() { RedirectUri = posted.RedirectUri, ClientId = clientId };
}
