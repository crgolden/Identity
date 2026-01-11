using Microsoft.AspNetCore.Identity;

namespace Identity.Data.Entities
{
    /// <inheritdoc />
    public class UserClaim : IdentityUserClaim<Guid>
    {
        /// <summary>Gets or sets the user.</summary>
        /// <value>The user.</value>
        public virtual User User { get; set; }
    }
}
