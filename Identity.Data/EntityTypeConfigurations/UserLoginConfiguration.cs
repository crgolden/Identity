using Identity.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Data.EntityTypeConfigurations
{
    using static ArgumentNullException;

    /// <inheritdoc />
    public class UserLoginConfiguration : IEntityTypeConfiguration<UserLogin>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<UserLogin> builder)
        {
            ThrowIfNull(builder);

            builder.HasOne(e => e.User)
                .WithMany(e => e.Logins)
                .HasForeignKey(ul => ul.UserId)
                .IsRequired();

            builder.ToTable("UserLogins");
        }
    }
}
