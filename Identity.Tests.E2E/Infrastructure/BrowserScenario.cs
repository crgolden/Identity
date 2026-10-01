namespace Identity.Tests.E2E.Infrastructure;

using Microsoft.Playwright;
using Reqnroll;

[Binding]
public sealed class BrowserScenario
{
    private readonly ScenarioContext _scenarioContext;
    private readonly List<IAsyncDisposable> _sessions = [];
    private PlaywrightFixture? _fixture;
    private IPage? _page;
    private string? _testId;

    public BrowserScenario(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    public PlaywrightFixture Fixture =>
        _fixture ?? throw new InvalidOperationException($"{nameof(Fixture)} is not available until the scenario has started.");

    public IPage Page =>
        _page ?? throw new InvalidOperationException($"{nameof(Page)} is not available until the scenario has started.");

    [BeforeScenario]
    public async Task OpenAsync()
    {
        _testId = TestContext.Current.Test?.UniqueID
            ?? throw new InvalidOperationException($"{nameof(BrowserScenario)} opened outside a running test.");
        _fixture = await TestContext.Current.GetFixture<PlaywrightFixture>()
            ?? throw new InvalidOperationException(
                $"No {nameof(PlaywrightFixture)} is attached to this scenario. Declare the generated feature class partial with [Collection({nameof(E2ECollection)}.{nameof(E2ECollection.Name)})].");
        _page = await OpenAnotherPageAsync();
    }

    public async Task<IPage> OpenAnotherPageAsync()
    {
        var (session, page) = await Fixture.NewPageAsync();
        _sessions.Add(session);
        return page;
    }

    [AfterScenario]
    public async Task CloseAsync()
    {
        try
        {
            await Task.WhenAll(_sessions.Select(session => session.DisposeAsync().AsTask()));
        }
        catch (Exception teardownError)
        {
            FinalizeArtifacts(teardownError);
            throw;
        }

        FinalizeArtifacts(null);
    }

    private void FinalizeArtifacts(Exception? teardownError)
    {
        if (_testId is not null)
        {
            PlaywrightArtifactRecorder.Finalize(_testId, _scenarioContext, teardownError);
        }
    }
}
