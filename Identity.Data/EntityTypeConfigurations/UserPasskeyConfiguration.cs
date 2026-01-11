using Identity.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Data.EntityTypeConfigurations
{
    using static ArgumentNullException;

    /// <inheritdoc />
    public class UserPasskeyConfiguration : IEntityTypeConfiguration<UserPasskey>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<UserPasskey> builder)
        {
            ThrowIfNull(builder);

            builder.HasOne(e => e.User)
                .WithMany(e => e.Passkeys)
                .HasForeignKey(up => up.UserId)
                .IsRequired();

            builder.ToTable("UserPasskeys");
        }
    }
}
