using Microsoft.AspNetCore.Identity;

namespace Identity.Data.Entities
{
    /// <inheritdoc />
    public class RoleClaim : IdentityRoleClaim<Guid>
    {
        /// <summary>Gets or sets the role.</summary>
        /// <value>The role.</value>
        public virtual Role Role { get; set; }
    }
}
