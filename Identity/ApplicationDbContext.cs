namespace Identity;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Extensions;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

public class ApplicationDbContext : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid, IdentityUserClaim<Guid>, IdentityUserRole<Guid>, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>, IdentityUserPasskey<Guid>>, IConfigurationDbContext, IPersistedGrantDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<ClientCorsOrigin> ClientCorsOrigins { get; set; }

    public virtual DbSet<IdentityResource> IdentityResources { get; set; }

    public virtual DbSet<ApiResource> ApiResources { get; set; }

    public virtual DbSet<ApiScope> ApiScopes { get; set; }

    public virtual DbSet<IdentityProvider> IdentityProviders { get; set; }

    public virtual DbSet<PersistedGrant> PersistedGrants { get; set; }

    public virtual DbSet<DeviceFlowCodes> DeviceFlowCodes { get; set; }

    public virtual DbSet<Key> Keys { get; set; }

    public virtual DbSet<ServerSideSession> ServerSideSessions { get; set; }

    public virtual DbSet<PushedAuthorizationRequest> PushedAuthorizationRequests { get; set; }

    public virtual DbSet<SamlServiceProvider> SamlServiceProviders { get; set; }

    public virtual DbSet<SamlSigninState> SamlSigninStates { get; set; }

    public virtual DbSet<SamlLogoutSession> SamlLogoutSessions { get; set; }

    public virtual DbSet<SamlLogoutSessionRequestIndex> SamlLogoutSessionRequestIndices { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ThrowIfNull(builder);
        var configurationStoreOptions = this.GetService<ConfigurationStoreOptions>();
        var operationalStoreOptions = this.GetService<OperationalStoreOptions>();
        builder.ConfigureClientContext(configurationStoreOptions);
        builder.ConfigureResourcesContext(configurationStoreOptions);
        builder.ConfigureIdentityProviderContext(configurationStoreOptions);
        builder.ConfigurePersistedGrantContext(operationalStoreOptions);
        base.OnModelCreating(builder);
    }
}