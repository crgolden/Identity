namespace Identity;

using System.Threading.Channels;

/// <summary>Hosted service that drains the background task channel, executing each work item in its own DI scope.</summary>
public class BackgroundTaskWorker : BackgroundService
{
    private readonly ChannelReader<Func<IServiceProvider, CancellationToken, Task>> _reader;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<BackgroundTaskWorker> _logger;

    public BackgroundTaskWorker(
        ChannelReader<Func<IServiceProvider, CancellationToken, Task>> reader,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<BackgroundTaskWorker> logger)
    {
        _reader = reader;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var workItem in _reader.ReadAllAsync(stoppingToken))
        {
            using var scope = _serviceScopeFactory.CreateScope();
            try
            {
                await workItem(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background task failed.");
            }
        }
    }
}
