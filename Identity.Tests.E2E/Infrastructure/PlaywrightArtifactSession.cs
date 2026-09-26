namespace Identity.Tests.E2E.Infrastructure;

using Microsoft.Playwright;

internal sealed class PlaywrightArtifactSession : IAsyncDisposable
{
    private readonly PlaywrightArtifactRecorder _recorder;
    private readonly IBrowserContext _context;
    private readonly IPage _page;
    private bool _disposed;

    public PlaywrightArtifactSession(PlaywrightArtifactRecorder recorder, IBrowserContext context, IPage page)
    {
        _recorder = recorder;
        _context = context;
        _page = page;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _recorder.CompleteAsync(_context, _page).ConfigureAwait(false);
    }
}
