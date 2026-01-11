using Identity.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Data.EntityTypeConfigurations
{
    using static ArgumentNullException;

    /// <inheritdoc />
    public class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<UserToken> builder)
        {
            ThrowIfNull(builder);

            builder.HasOne(e => e.User)
                .WithMany(e => e.Tokens)
                .HasForeignKey(ut => ut.UserId)
                .IsRequired();

            builder.ToTable("UserTokens");
        }
    }
}
