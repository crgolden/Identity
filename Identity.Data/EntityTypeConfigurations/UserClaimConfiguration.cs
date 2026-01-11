using Identity.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Data.EntityTypeConfigurations
{
    using static ArgumentNullException;

    /// <inheritdoc />
    public class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<UserClaim> builder)
        {
            ThrowIfNull(builder);

            builder.HasOne(e => e.User)
                .WithMany(e => e.Claims)
                .HasForeignKey(uc => uc.UserId)
                .IsRequired();

            builder.ToTable("UserClaims");
        }
    }
}
