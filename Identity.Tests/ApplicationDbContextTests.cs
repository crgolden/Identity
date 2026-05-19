namespace Identity.Tests;
using Infrastructure;

using Microsoft.EntityFrameworkCore;

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

    public static TheoryData<DbContextOptions<ApplicationDbContext>> ValidOptions() => new()
    {
        new DbContextOptionsBuilder<ApplicationDbContext>().Options,
    };

#pragma warning disable xUnit1045
    [Theory]
    [MemberData(nameof(ValidOptions))]
    public void Constructor_ValidOptions_CreatesInstance(DbContextOptions<ApplicationDbContext> options)
    {
        // Arrange
        // (options provided by MemberData)

        // Act
        var context = new ApplicationDbContext(options);

        // Assert
        Assert.NotNull(context);
        Assert.IsType<ApplicationDbContext>(context);
    }
#pragma warning restore xUnit1045

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        DbContextOptions<ApplicationDbContext>? options = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ApplicationDbContext(options!));
    }
}