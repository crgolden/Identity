namespace Identity.Tests.E2E;

using System.Collections;
using System.Reflection;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Stores;
using Duende.IdentityServer.Stores;
using Identity.Tests.E2E.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[Trait("Category", "E2E")]
[Collection(E2ECollection.Name)]
public sealed class SingleQueryClientStoreTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task ClientStoreRegistration_ResolvesTheSingleQueryStoreBehindDuendesValidation()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();

        var store = scope.ServiceProvider.GetRequiredService<IClientStore>();

        Assert.IsType<ValidatingClientStore<SingleQueryClientStore>>(store);
    }

    [Fact]
    public async Task SeedClientAsync_FillsEveryCollectionNavigationTheModelDeclares_SoTheParityTestCannotPassVacuously()
    {
        var rowsPerCollection = Random.Shared.Next(2, 5);
        var clientId = await SeedClientAsync(rowsPerCollection);
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var navigations = db.Clients.EntityType.GetNavigations().ToList();

        var seeded = await navigations
            .Aggregate(db.Clients.Where(client => client.ClientId == clientId), (clients, navigation) => clients.Include(navigation.Name))
            .AsNoTracking()
            .AsSingleQuery()
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.NotEmpty(navigations);
        Assert.All(
            navigations,
            navigation => Assert.Equal(
                rowsPerCollection,
                Assert.IsType<ICollection>(Assert.IsType<PropertyInfo>(navigation.PropertyInfo, exactMatch: false).GetValue(seeded), exactMatch: false).Count));
    }

    [Fact]
    public async Task FindClientByIdAsync_ReturnsTheSameClientAsDuendesStore()
    {
        var rowsPerCollection = Random.Shared.Next(2, 5);
        var clientId = await SeedClientAsync(rowsPerCollection);
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var duendeStore = scope.ServiceProvider.GetRequiredService<ClientStore>();
        var singleQueryStore = scope.ServiceProvider.GetRequiredService<SingleQueryClientStore>();
        var expected = await duendeStore.FindClientByIdAsync(clientId, TestContext.Current.CancellationToken);

        var actual = await singleQueryStore.FindClientByIdAsync(clientId, TestContext.Current.CancellationToken);

        Assert.NotNull(expected);
        Assert.Equivalent(expected, actual, strict: true);
    }

    [Fact]
    public async Task FindClientByIdAsync_WhenTheIdDiffersOnlyInCase_ReturnsNullLikeDuendesStore()
    {
        var rowsPerCollection = Random.Shared.Next(2, 5);
        var clientId = await SeedClientAsync(rowsPerCollection);
        var caseVariant = clientId.ToUpperInvariant();
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var duendeStore = scope.ServiceProvider.GetRequiredService<ClientStore>();
        var singleQueryStore = scope.ServiceProvider.GetRequiredService<SingleQueryClientStore>();
        var collationIgnoresCase = await fixture.AnyAsync<Client>(client => client.ClientId == caseVariant);
        var expected = await duendeStore.FindClientByIdAsync(caseVariant, TestContext.Current.CancellationToken);

        var actual = await singleQueryStore.FindClientByIdAsync(caseVariant, TestContext.Current.CancellationToken);

        Assert.True(collationIgnoresCase, "the database matched no case variant, so this test cannot tell the in-memory ordinal filter from the SQL one");
        Assert.Null(expected);
        Assert.Null(actual);
    }

    [Fact]
    public async Task FindClientByIdAsync_ReadsTheClientAndEveryCollectionInOneCommand()
    {
        var rowsPerCollection = Random.Shared.Next(2, 5);
        var clientId = await SeedClientAsync(rowsPerCollection);
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var singleQueryStore = scope.ServiceProvider.GetRequiredService<SingleQueryClientStore>();
        var commandsBeforeLookup = fixture.Factory.Commands.CommandsCarrying(clientId).Count;

        await singleQueryStore.FindClientByIdAsync(clientId, TestContext.Current.CancellationToken);

        Assert.Single(fixture.Factory.Commands.CommandsCarrying(clientId).Skip(commandsBeforeLookup));
    }

    [Fact]
    public async Task DuendesClientStore_ReadsTheClientInOneCommandPerCollectionPlusOne_SoTheCommandCountDiscriminates()
    {
        var rowsPerCollection = Random.Shared.Next(2, 5);
        var clientId = await SeedClientAsync(rowsPerCollection);
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var duendeStore = scope.ServiceProvider.GetRequiredService<ClientStore>();
        var collectionCount = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Clients.EntityType.GetNavigations().Count();
        var commandsBeforeLookup = fixture.Factory.Commands.CommandsCarrying(clientId).Count;

        await duendeStore.FindClientByIdAsync(clientId, TestContext.Current.CancellationToken);

        Assert.Equal(collectionCount + 1, fixture.Factory.Commands.CommandsCarrying(clientId).Count - commandsBeforeLookup);
    }

    private static List<TRow> Rows<TRow>(int count, Func<TRow> row) =>
        [.. Enumerable.Range(0, count).Select(_ => row())];

    private async Task<string> SeedClientAsync(int rowsPerCollection)
    {
        var clientId = Generated.NewToken();
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Clients.Add(new Client
        {
            ClientId = clientId,
            ClientName = Generated.NewDisplayName(),
            ProtocolType = OidcStandardConstants.OidcProtocol,
            AllowedCorsOrigins = Rows(rowsPerCollection, () => new ClientCorsOrigin { Origin = Generated.NewToken() }),
            AllowedGrantTypes = Rows(rowsPerCollection, () => new ClientGrantType { GrantType = Generated.NewToken() }),
            AllowedScopes = Rows(rowsPerCollection, () => new ClientScope { Scope = Generated.NewToken() }),
            Claims = Rows(rowsPerCollection, () => new ClientClaim { Type = Generated.NewToken(), Value = Generated.NewClaimValue() }),
            ClientSecrets = Rows(rowsPerCollection, () => new ClientSecret { Value = Generated.NewSecretValue() }),
            IdentityProviderRestrictions = Rows(rowsPerCollection, () => new ClientIdPRestriction { Provider = Generated.NewToken() }),
            PostLogoutRedirectUris = Rows(rowsPerCollection, () => new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = Generated.NewToken() }),
            Properties = Rows(rowsPerCollection, () => new ClientProperty { Key = Generated.NewToken(), Value = Generated.NewClaimValue() }),
            RedirectUris = Rows(rowsPerCollection, () => new ClientRedirectUri { RedirectUri = Generated.NewToken() }),
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return clientId;
    }
}
