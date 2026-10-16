namespace Identity.Tests.Unit;

using System.Reflection;
using Duende.IdentityServer.EntityFramework.Options;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ApplicationDbContextTests
{
    public static TheoryData<Type> EntityTypes() => new()
    {
        typeof(Duende.IdentityServer.EntityFramework.Entities.Client),
        typeof(Duende.IdentityServer.EntityFramework.Entities.ClientCorsOrigin),
        typeof(Duende.IdentityServer.EntityFramework.Entities.IdentityResource),
        typeof(Duende.IdentityServer.EntityFramework.Entities.ApiResource),
        typeof(Duende.IdentityServer.EntityFramework.Entities.ApiScope),
        typeof(Duende.IdentityServer.EntityFramework.Entities.IdentityProvider),
        typeof(Duende.IdentityServer.EntityFramework.Entities.PersistedGrant),
        typeof(Duende.IdentityServer.EntityFramework.Entities.DeviceFlowCodes),
        typeof(Duende.IdentityServer.EntityFramework.Entities.Key),
        typeof(Duende.IdentityServer.EntityFramework.Entities.ServerSideSession),
        typeof(Duende.IdentityServer.EntityFramework.Entities.PushedAuthorizationRequest),
    };

    [Fact]
    public void Constructor_ValidOptions_CreatesInstance()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().Options;

        // Act
        var context = new ApplicationDbContext(options);

        // Assert
        Assert.NotNull(context);
        Assert.IsType<ApplicationDbContext>(context);
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        DbContextOptions<ApplicationDbContext>? options = null;

        // Act
        var exception = Record.Exception(() => new ApplicationDbContext(options));

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public void OnModelCreating_NullBuilder_ThrowsNamingTheBuilder()
    {
        // Arrange
        using var context = ContextWithStoreOptions();
        var onModelCreating = typeof(ApplicationDbContext)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Single(method => method.GetParameters() is [{ ParameterType: var parameterType }] && parameterType == typeof(ModelBuilder));
        var builderParameter = onModelCreating.GetParameters().Single().Name;

        // Act
        var exception = Assert.Throws<TargetInvocationException>(() => onModelCreating.Invoke(context, [null]));

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(exception.InnerException);
        Assert.Equal(builderParameter, argumentNull.ParamName);
    }

    [Fact]
    public void Model_ConfiguresTheOperationalStoreKeys()
    {
        // Arrange
        using var context = ContextWithStoreOptions();

        // Act
        var persistedGrant = context.Model.FindEntityType(typeof(Duende.IdentityServer.EntityFramework.Entities.PersistedGrant));

        // Assert
        Assert.NotNull(persistedGrant?.FindPrimaryKey());
    }

    [Fact]
    public void Model_ConfiguresTheClientIdAsAUniqueIndex()
    {
        // Arrange
        using var context = ContextWithStoreOptions();

        // Act
        var client = context.Model.FindEntityType(typeof(Duende.IdentityServer.EntityFramework.Entities.Client));

        // Assert
        Assert.True(HasUniqueIndexOn(client, nameof(Duende.IdentityServer.EntityFramework.Entities.Client.ClientId)));
    }

    [Fact]
    public void Model_ConfiguresTheApiResourceNameAsAUniqueIndex()
    {
        // Arrange
        using var context = ContextWithStoreOptions();

        // Act
        var apiResource = context.Model.FindEntityType(typeof(Duende.IdentityServer.EntityFramework.Entities.ApiResource));

        // Assert
        Assert.True(HasUniqueIndexOn(apiResource, nameof(Duende.IdentityServer.EntityFramework.Entities.ApiResource.Name)));
    }

    [Fact]
    public void Model_ConfiguresTheIdentityProviderSchemeAsAUniqueIndex()
    {
        // Arrange
        using var context = ContextWithStoreOptions();

        // Act
        var identityProvider = context.Model.FindEntityType(typeof(Duende.IdentityServer.EntityFramework.Entities.IdentityProvider));

        // Assert
        Assert.True(HasUniqueIndexOn(identityProvider, nameof(Duende.IdentityServer.EntityFramework.Entities.IdentityProvider.Scheme)));
    }

    [Fact]
    public void Model_KeepsTheAspNetIdentityTables()
    {
        // Arrange
        using var context = ContextWithStoreOptions();

        // Act
        var user = context.Model.FindEntityType(typeof(Microsoft.AspNetCore.Identity.IdentityUser<Guid>));

        // Assert
        Assert.True(HasUniqueIndexOn(user, nameof(Microsoft.AspNetCore.Identity.IdentityUser<Guid>.NormalizedUserName)));
    }

    private static ApplicationDbContext ContextWithStoreOptions()
    {
        var applicationServices = new ServiceCollection()
            .AddSingleton(new ConfigurationStoreOptions())
            .AddSingleton(new OperationalStoreOptions())
            .BuildServiceProvider();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer()
            .UseApplicationServiceProvider(applicationServices)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static bool HasUniqueIndexOn(IEntityType? entityType, string propertyName) =>
        entityType?.GetIndexes().Any(index => index.IsUnique
            && index.Properties.Count == 1
            && string.Equals(index.Properties[0].Name, propertyName, StringComparison.Ordinal)) == true;
}
