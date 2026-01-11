using Microsoft.AspNetCore.Identity;

namespace Identity.Data.Entities
{
    /// <inheritdoc />
    public class User : IdentityUser<Guid>
    {
        /// <inheritdoc />
        public User()
        {
        }

        /// <inheritdoc />
        public User(string userName)
            : base(userName)
        {
        }

        /// <summary>Gets the claims.</summary>
        /// <value>The claims.</value>
        public virtual ICollection<UserClaim> Claims { get; } = new List<UserClaim>();

        /// <summary>Gets the logins.</summary>
        /// <value>The logins.</value>
        public virtual ICollection<UserLogin> Logins { get; } = new List<UserLogin>();

        /// <summary>Gets the passkeys.</summary>
        /// <value>The passkeys.</value>
        public virtual ICollection<UserPasskey> Passkeys { get; } = new List<UserPasskey>();

        /// <summary>Gets the tokens.</summary>
        /// <value>The tokens.</value>
        public virtual ICollection<UserToken> Tokens { get; } = new List<UserToken>();

        /// <summary>Gets the user roles.</summary>
        /// <value>The user roles.</value>
        public virtual ICollection<UserRole> UserRoles { get; } = new List<UserRole>();
    }
}
