namespace Identity;

/// <summary>
/// Provides avatar lookup functionality for user or profile identifiers.
/// Implementations return a publicly accessible <see cref="Uri"/> pointing to an avatar image,
/// or <c>null</c> when no avatar is available for the given identifier.
/// </summary>
public interface IAvatarService
{
    /// <summary>
    /// Asynchronously obtains an avatar <see cref="Uri"/> for the specified profile identifier.
    /// </summary>
    /// <param name="profileIdentifier">A unique identifier for the profile (user id, username, email hash, etc.).</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>
    /// A task that completes with a <see cref="Uri"/> pointing to the avatar image, or <c>null</c> if none exists.
    /// Implementations should not throw for missing avatars; use <c>null</c> instead.
    /// </returns>
    Task<Uri?> GetAvatarUrlAsync(string profileIdentifier, CancellationToken cancellationToken = default);
}
