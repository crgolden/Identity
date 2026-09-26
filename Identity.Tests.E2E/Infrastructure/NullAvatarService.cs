namespace Identity.Tests.E2E.Infrastructure;

using Identity.Avatar;

internal sealed class NullAvatarService : IAvatarService
{
    public Task<Uri?> GetAvatarUrlAsync(string profileIdentifier, CancellationToken cancellationToken = default)
        => Task.FromResult<Uri?>(null);

    public bool IsOwnComputedUrl(string candidate) => false;
}
