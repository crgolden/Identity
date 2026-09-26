namespace Identity.Avatar;

using System.Security.Cryptography;

public class GravatarService : IAvatarService
{
    internal const string ActivityName = "identity.gravatar.build_url";
    internal const string HashTagName = "gravatar.hash";
    internal const string DefaultImageQuery = "?s=2048&d=identicon";
    internal const string GravatarHost = "gravatar.com";
    internal const string ImagePath = "/avatar/";

    internal static readonly string ImageBaseUrl = Uri.UriSchemeHttps + Uri.SchemeDelimiter + GravatarHost + ImagePath;

    private readonly Telemetry _telemetry;

    public GravatarService(Telemetry telemetry)
    {
        _telemetry = telemetry;
    }

    public Task<Uri?> GetAvatarUrlAsync(string profileIdentifier, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var hash = HashIdentifier(profileIdentifier);
        using var activity = _telemetry.StartActivity(ActivityName);
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
