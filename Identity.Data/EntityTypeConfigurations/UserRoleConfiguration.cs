using Identity.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Data.EntityTypeConfigurations
{
    using static ArgumentNullException;

    /// <inheritdoc />
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            ThrowIfNull(builder);

            builder.HasKey(e => new { e.UserId, e.RoleId });

            builder.HasOne(e => e.User)
                .WithMany(ur => ur.UserRoles)
                .HasPrincipalKey(u => u.Id)
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();

            builder.HasOne(e => e.Role)
                .WithMany(ur => ur.UserRoles)
                .HasPrincipalKey(r => r.Id)
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired();

            builder.ToTable("UserRoles");
        }
    }
}
