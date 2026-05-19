namespace Identity;

using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Channels;
using Microsoft.AspNetCore.Identity;

/// <summary>Hosted service that drains the picture claim channel, adding a picture claim for each user.</summary>
public class PictureClaimWorker : BackgroundService
{
    private readonly ChannelReader<string> _pictureClaimReader;
    private readonly IServiceScopeFactory _scopeFactory;

    public PictureClaimWorker(
        ChannelReader<string> pictureClaimReader,
        IServiceScopeFactory scopeFactory)
    {
        _pictureClaimReader = pictureClaimReader;
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var name in _pictureClaimReader.ReadAllAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            try
            {
                var avatarService = scope.ServiceProvider.GetRequiredService<IAvatarService>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();

                var avatarUrl = await avatarService.GetAvatarUrlAsync(name, stoppingToken);
                if (avatarUrl is null)
                {
                    continue;
                }

                var user = await userManager.FindByNameAsync(name);
                if (user is null)
                {
                    continue;
                }

                var claims = await userManager.GetClaimsAsync(user);
                if (claims.Any(x => string.Equals("picture", x.Type, StringComparison.Ordinal)))
                {
                    continue;
                }

                await userManager.AddClaimAsync(user, new Claim("picture", avatarUrl.ToString()));
            }
            catch (Exception ex)
            {
                Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
                Activity.Current?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
                {
                    { "exception.type", ex.GetType().FullName },
                    { "exception.message", ex.Message },
                }));
            }
        }
    }
}
