namespace Identity.Avatar;

public interface IAvatarService
{
    Task<Uri?> GetAvatarUrlAsync(string profileIdentifier, CancellationToken cancellationToken = default);
}