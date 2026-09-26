namespace Identity.Pages.Admin.Clients.Edit;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;

public class PostLogoutRedirectUris : EditableClientResourcesBase<ClientPostLogoutRedirectUri>
{
    internal const string DetailsPageName = "/Admin/Clients/Details/PostLogoutRedirectUris";

    public PostLogoutRedirectUris(IConfigurationDbContext context)
        : base(context)
    {
    }

    protected override Expression<Func<Client, List<ClientPostLogoutRedirectUri>>> Collection => c => c.PostLogoutRedirectUris;

    protected override string DetailsPage => DetailsPageName;

    protected override int IdOf(ClientPostLogoutRedirectUri resource) => resource.Id;

    protected override void CopyEditableFields(ClientPostLogoutRedirectUri posted, ClientPostLogoutRedirectUri existing) => existing.PostLogoutRedirectUri = posted.PostLogoutRedirectUri;

    protected override ClientPostLogoutRedirectUri CreateForClient(ClientPostLogoutRedirectUri posted, int clientId) =>
        new() { PostLogoutRedirectUri = posted.PostLogoutRedirectUri, ClientId = clientId };
}
