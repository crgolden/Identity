using Identity.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Data.EntityTypeConfigurations
{
    using static ArgumentNullException;

    /// <inheritdoc />
    public class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<RoleClaim> builder)
        {
            ThrowIfNull(builder);

            builder.HasOne(e => e.Role)
                .WithMany(e => e.RoleClaims)
                .HasForeignKey(rc => rc.RoleId)
                .IsRequired();

            builder.ToTable("RoleClaims");
        }
    }
}
