namespace Identity;

using System.Diagnostics;
using System.Security.Cryptography;

/// <summary>Gravatar-backed implementation of <see cref="IAvatarService"/>.</summary>
public class GravatarService : IAvatarService
{
    private readonly IGravatar _gravatar;

    public GravatarService(IGravatar gravatar)
    {
        _gravatar = gravatar;
    }

    /// <inheritdoc/>
    public async Task<Uri?> GetAvatarUrlAsync(string profileIdentifier, CancellationToken cancellationToken = default)
    {
        var source = UTF8.GetBytes(profileIdentifier);
        var inArray = SHA256.HashData(source);
        var hash = System.Convert.ToHexString(inArray);
        using var activity = Telemetry.ActivitySource.StartActivity("identity.gravatar.get_profile");
        activity?.SetTag("gravatar.hash", hash.ToLowerInvariant());
        try
        {
            var profile = await _gravatar.GetProfileByIdAsync(hash.ToLowerInvariant(), cancellationToken);
            return profile?.Avatar_url;
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
    }
}
