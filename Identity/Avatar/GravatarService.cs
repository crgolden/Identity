namespace Identity.Avatar;

using System.Security.Cryptography;

public class GravatarService : IAvatarService
{
    internal const string ActivityName = "identity.gravatar.build_url";
    internal const string HashTagName = "gravatar.hash";
#pragma warning disable S1075 // Fixed vendor endpoint, not configuration — see CODE-STYLE.md's "Configuration must not decide control flow" corollary.
    internal const string ImageBaseUrl = "https://gravatar.com/avatar/";
#pragma warning restore S1075
    internal const string DefaultImageQuery = "?s=2048&d=identicon";
    internal const string GravatarHost = "gravatar.com";

    public Task<Uri?> GetAvatarUrlAsync(string profileIdentifier, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var hash = HashIdentifier(profileIdentifier);
        using var activity = Telemetry.StartActivity(ActivityName);
        activity?.SetTag(HashTagName, hash);

        var url = new Uri($"{ImageBaseUrl}{hash}{DefaultImageQuery}");
        return Task.FromResult<Uri?>(url);
    }

    public bool IsOwnComputedUrl(string candidate)
    {
        return Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            && (string.Equals(uri.Host, GravatarHost, StringComparison.OrdinalIgnoreCase)
                || uri.Host.EndsWith($".{GravatarHost}", StringComparison.OrdinalIgnoreCase));
    }

    internal static string HashIdentifier(string profileIdentifier)
    {
        var source = UTF8.GetBytes(profileIdentifier.Trim().ToLowerInvariant());
        var inArray = SHA256.HashData(source);
        return System.Convert.ToHexString(inArray).ToLowerInvariant();
    }
}
