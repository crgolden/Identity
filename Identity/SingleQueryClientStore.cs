namespace Identity;

using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.EntityFramework.Stores;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed class SingleQueryClientStore : ClientStore
{
    public SingleQueryClientStore(IConfigurationDbContext context, ILogger<SingleQueryClientStore> logger)
        : base(context, logger)
    {
    }

    public override async Task<Client?> FindClientByIdAsync(string clientId, CancellationToken ct)
    {
        var query = Context.Clients.EntityType.GetNavigations().Aggregate(
            Context.Clients.Where(client => client.ClientId == clientId),
            (clients, navigation) => clients.Include(navigation.Name));

        var candidates = await query.AsNoTracking().AsSingleQuery().ToArrayAsync(ct);
        return candidates
            .SingleOrDefault(client => string.Equals(client.ClientId, clientId, StringComparison.Ordinal))
            ?.ToModel();
    }
}
