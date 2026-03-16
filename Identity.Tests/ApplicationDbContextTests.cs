using Microsoft.EntityFrameworkCore;

namespace Identity.Tests;

/// <summary>
/// Tests for Identity.ApplicationDbContext.OnModelCreating behavior.
/// </summary>
public class ApplicationDbContextTests
{
    /// <summary>
    /// Provide a representative set of entity CLR types that the Configure* extension methods should add to the model.
    /// These are likely to be registered when OnModelCreating is executed with non-null store options.
    /// </summary>
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

    /// <summary>
    /// Testable subclass to expose the protected OnModelCreating for direct invocation.
    /// It lives inside the test class to avoid creating external helper types.
    /// </summary>
    private class TestableApplicationDbContext : ApplicationDbContext
    {
        public TestableApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Exposes the protected OnModelCreating for testing.
        /// </summary>
        public void InvokeOnModelCreating(ModelBuilder builder) => base.OnModelCreating(builder);
    }

    /// <summary>
    /// Verifies that the constructor successfully creates an instance when provided with valid DbContextOptions.
    /// Input: a non-null <see cref="DbContextOptions{ApplicationDbContext}"/> produced by <see cref="DbContextOptionsBuilder{TContext}"/>.
    /// Expected: an instance of <see cref="ApplicationDbContext"/> is returned and no exception is thrown.
    /// </summary>
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

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> when called with null options.
    /// Input: null for the non-nullable <see cref="DbContextOptions{ApplicationDbContext}"/> parameter.
    /// Expected: <see cref="ArgumentNullException"/> is thrown.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        DbContextOptions<ApplicationDbContext>? options = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ApplicationDbContext(options!));
    }

    /// <summary>
    /// Provides valid DbContextOptions instances for parameterized tests.
    /// Currently provides a default-built options instance. This covers the typical construction scenario
    /// where options may not include a provider. Additional provider-backed options can be added if available.
    /// </summary>
    public static TheoryData<DbContextOptions<ApplicationDbContext>> ValidOptions() => new()
    {
        new DbContextOptionsBuilder<ApplicationDbContext>().Options,
    };
}