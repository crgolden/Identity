namespace Identity;

using System.Security.Cryptography;

public class GravatarService : IAvatarService
{
    private readonly IGravatar _gravatar;

    public GravatarService(IGravatar gravatar)
    {
        _gravatar = gravatar;
    }

    public async Task<Uri?> GetAvatarUrlAsync(string profileIdentifier, CancellationToken cancellationToken = default)
    {
        var source = UTF8.GetBytes(profileIdentifier);
        var inArray = SHA256.HashData(source);
        var hash = System.Convert.ToHexString(inArray);
        var profile = await _gravatar.GetProfileByIdAsync(hash.ToLowerInvariant(), cancellationToken);
        return profile?.Avatar_url;
    }
}
