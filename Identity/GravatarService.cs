namespace Identity;

using System.Security.Cryptography;
using System.Text;

/// <inheritdoc />
public class GravatarService : IAvatarService
{
    private readonly IGravatar _gravatar;

    /// <inheritdoc cref="IAvatarService"/>
    public GravatarService(IGravatar gravatar)
    {
        _gravatar = gravatar;
    }

    /// <inheritdoc />
    public async Task<Uri?> GetAvatarUrlAsync(string profileIdentifier, CancellationToken cancellationToken = default)
    {
        var source = Encoding.UTF8.GetBytes(profileIdentifier);
        var inArray = SHA256.HashData(source);
        var hash = Convert.ToHexString(inArray);
        var profile = await _gravatar.GetProfileByIdAsync(hash.ToLowerInvariant(), cancellationToken);
        return profile?.Avatar_url;
    }
}
