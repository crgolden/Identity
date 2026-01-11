using Identity.Data.Entities;

namespace Identity.Data
{
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using static ArgumentNullException;
    using static System.Reflection.Assembly;

    /// <inheritdoc />
    public class ApplicationDbContext : IdentityDbContext<User, Role, Guid, UserClaim, UserRole, UserLogin, RoleClaim, UserToken, UserPasskey>
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

            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(GetExecutingAssembly());
        }
    }
}
