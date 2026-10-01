namespace Identity.Tests.E2E.Infrastructure;

using Identity.CAPTCHA;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

public sealed class PlaywrightFixture : IdentityHostFixture
{
    internal const string AppArtifactName = nameof(Identity);

    private static readonly bool StrykerActive = Environment.GetEnvironmentVariable("STRYKER_MUTANT_FILE") is not null;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private PlaywrightSettings? _playwrightSettings;
    private Uri? _recaptchaScriptEndpoint;

    public override async ValueTask InitializeAsync()
    {
        if (StrykerActive)
        {
            return;
        }

        await base.InitializeAsync();
        _playwrightSettings = PlaywrightSettings.Read(Factory.Services.GetRequiredService<IConfiguration>());
        _recaptchaScriptEndpoint = Factory.Services.GetRequiredService<ICAPTCHAService>().ScriptEndpoint
            ?? throw new InvalidOperationException($"{nameof(ICAPTCHAService.ScriptEndpoint)} is not configured.");

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _playwrightSettings.Headless
        });

        var (warmupCtx, warmupPage) = await NewPageAsync();
        await using (warmupCtx)
        {
            await warmupPage.GotoAsync(PageRoutes.Login);
        }
    }

    public async Task<(IAsyncDisposable Context, IPage Page)> NewPageAsync(PlaywrightSuite suite = PlaywrightSuite.E2E)
    {
        if (_browser is null || _playwrightSettings is null || _recaptchaScriptEndpoint is null)
        {
            throw new InvalidOperationException("Browser is not initialized. Ensure InitializeAsync has been awaited.");
        }

        var contextOptions = new BrowserNewContextOptions
        {
            BaseURL = BaseAddress,
            IgnoreHTTPSErrors = true
        };
        var (session, page) = await PlaywrightArtifactRecorder.CreateSessionAsync(
            _browser, AppArtifactName, suite.ToString(), contextOptions, _playwrightSettings);

        await page.Context.AddInitScriptAsync(BrowserScripts.GrecaptchaStub);
        await page.Context.RouteAsync($"{_recaptchaScriptEndpoint}**", route => route.AbortAsync());

        return (session, page);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        await base.DisposeAsyncCore();
    }
}
