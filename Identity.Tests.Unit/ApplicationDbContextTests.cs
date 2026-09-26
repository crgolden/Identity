namespace Identity.Tests.Unit;

using Identity.Tests.Unit.Infrastructure;
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
}
