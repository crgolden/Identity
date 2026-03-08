namespace Identity;

public interface IAvatarService
{
    Task<Uri?> GetAvatarUrlAsync(string profileIdentifier, CancellationToken cancellationToken = default);
}
