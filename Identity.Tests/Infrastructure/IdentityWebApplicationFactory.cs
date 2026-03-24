namespace Identity.Tests.Infrastructure;

using System.Net;
using Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> for E2E tests.
/// Starts a real Kestrel server on a random port (for Playwright) alongside the
/// in-process TestServer. Replaces <see cref="IEmailSender"/> with
/// <see cref="EmailCaptureService"/> and Serilog with a console logger to avoid
/// external-service failures during startup.
/// </summary>
public sealed class IdentityWebApplicationFactory : WebApplicationFactory<Program>
{
    private IHost? _kestrelHost;
    private string? _serverAddress;

    public EmailCaptureService EmailCapture { get; } = new();

    public string ServerAddress => _serverAddress ?? throw new InvalidOperationException("Server address is not available. Call Factory.CreateClient() first.");

    /// <inheritdoc/>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Build the in-memory TestHost used by Factory.CreateClient().
        var testHost = builder.Build();

        // Build a second host with a real Kestrel socket for Playwright.
        builder.ConfigureWebHost(b => b.UseKestrel(o => o.Listen(IPAddress.Loopback, 0, lo => lo.UseHttps())));
        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        var server = _kestrelHost.Services.GetRequiredService<IServer>();
        var addresses = server.Features.GetRequiredFeature<IServerAddressesFeature>();
        _serverAddress = addresses.Addresses.First().TrimEnd('/');

        return testHost;
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices((context, services) =>
        {
            // Prevent a transient background-service exception (e.g. IdentityServer key refresh,
            // token cleanup) from stopping the Kestrel host mid-test-run and causing
            // ERR_CONNECTION_FAILED on subsequent Playwright navigations.
            // .NET 6+ default is StopHost; override to Ignore so the server stays alive.
            services.Configure<HostOptions>(opts =>
                opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

            if (!context.HostingEnvironment.IsProduction())
            {
                // Replace the Serilog ILoggerFactory (which connects to Elasticsearch on startup)
                // with the default logging infrastructure to avoid external sink failures in tests.
                services.RemoveAll<ILoggerFactory>();
                services.AddLogging(lb => lb.AddConsole());
            }

            // Remove the real IEmailSender registrations and replace with the capture service.
            services.RemoveAll<IEmailSender>();
            services.RemoveAll<IEmailSender<IdentityUser<Guid>>>();

            services.AddSingleton<EmailCaptureService>(_ => EmailCapture);
            services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<EmailCaptureService>());
            services.AddSingleton<IEmailSender<IdentityUser<Guid>>>(sp => sp.GetRequiredService<EmailCaptureService>());

            // Replace IAvatarService with a no-op stub to avoid real Gravatar HTTP calls in tests.
            services.RemoveAll<IAvatarService>();
            services.AddSingleton<IAvatarService>(new NullAvatarService());

            // Reduce PBKDF2 iterations to 1 for tests — default 600k iterations is CPU-intensive
            // and causes 60s+ timeouts on loaded CI machines (password sign-in late in the suite).
            services.Configure<PasswordHasherOptions>(opts => opts.IterationCount = 1);
        });
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _kestrelHost?.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>No-op <see cref="IAvatarService"/> for tests — avoids real Gravatar HTTP calls.</summary>
internal sealed class NullAvatarService : IAvatarService
{
    public Task<Uri?> GetAvatarUrlAsync(string profileIdentifier, CancellationToken cancellationToken = default)
        => Task.FromResult<Uri?>(null);
}