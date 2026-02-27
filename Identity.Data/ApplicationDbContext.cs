namespace Identity.Data
{
    using Duende.IdentityServer.EntityFramework.Entities;
    using Duende.IdentityServer.EntityFramework.Extensions;
    using Duende.IdentityServer.EntityFramework.Interfaces;
    using Duende.IdentityServer.EntityFramework.Options;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using static ArgumentNullException;

    /// <inheritdoc cref="DbContext" />
    public class ApplicationDbContext : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid, IdentityUserClaim<Guid>, IdentityUserRole<Guid>, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>, IdentityUserPasskey<Guid>>, IConfigurationDbContext, IPersistedGrantDbContext
    {
        /// <inheritdoc />
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public virtual DbSet<Client> Clients { get; set; }

        /// <inheritdoc />
        public virtual DbSet<ClientCorsOrigin> ClientCorsOrigins { get; set; }

        /// <inheritdoc />
        public virtual DbSet<IdentityResource> IdentityResources { get; set; }

        /// <inheritdoc />
        public virtual DbSet<ApiResource> ApiResources { get; set; }

        /// <inheritdoc />
        public virtual DbSet<ApiScope> ApiScopes { get; set; }

        /// <inheritdoc />
        public virtual DbSet<IdentityProvider> IdentityProviders { get; set; }

        /// <inheritdoc />
        public virtual DbSet<PersistedGrant> PersistedGrants { get; set; }

        /// <inheritdoc />
        public virtual DbSet<DeviceFlowCodes> DeviceFlowCodes { get; set; }

        /// <inheritdoc />
        public virtual DbSet<Key> Keys { get; set; }

        /// <inheritdoc />
        public virtual DbSet<ServerSideSession> ServerSideSessions { get; set; }

        /// <inheritdoc />
        public virtual DbSet<PushedAuthorizationRequest> PushedAuthorizationRequests { get; set; }
    }
}
