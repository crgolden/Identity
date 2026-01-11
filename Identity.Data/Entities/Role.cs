using Microsoft.AspNetCore.Identity;

namespace Identity.Data.Entities
{
    /// <inheritdoc />
    public class Role : IdentityRole<Guid>
    {
        /// <inheritdoc />
        public Role()
        {
        }

        /// <inheritdoc />
        public Role(string roleName)
            : base(roleName)
        {
        }

        /// <summary>Gets the user roles.</summary>
        /// <value>The user roles.</value>
        public virtual ICollection<UserRole> UserRoles { get; } = new List<UserRole>();

        /// <summary>Gets the role claims.</summary>
        /// <value>The role claims.</value>
        public virtual ICollection<RoleClaim> RoleClaims { get; } = new List<RoleClaim>();
    }
}
