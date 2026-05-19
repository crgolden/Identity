namespace Identity.Tests.Infrastructure;

using System.Net;
using Azure.Messaging.ServiceBus;
using Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class IdentityWebApplicationFactory : WebApplicationFactory<Program>
{
    private IHost? _kestrelHost;
    private string? _serverAddress;

    public EmailCaptureSender EmailCapture { get; } = new();

    public string ServerAddress => _serverAddress ?? throw new InvalidOperationException("Server address is not available. Call Factory.CreateClient() first.");

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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(configBuilder =>
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceBusConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="
            }));

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

            // Replace the real ServiceBusSender factory with the in-memory capture sender.
            services.RemoveAll<IAzureClientFactory<ServiceBusSender>>();
            services.AddSingleton(EmailCapture);
            services.AddSingleton<IAzureClientFactory<ServiceBusSender>>(new TestServiceBusSenderFactory(EmailCapture));

            // Replace IAvatarService with a no-op stub to avoid real Gravatar HTTP calls in tests.
            services.RemoveAll<IAvatarService>();
            services.AddSingleton<IAvatarService>(new NullAvatarService());

            // Replace ICAPTCHAService with an always-pass stub to prevent outbound calls to Google.
            services.RemoveAll<ICAPTCHAService>();
            services.AddSingleton<ICAPTCHAService>(new AlwaysPassCAPTCHAService());

            // Reduce PBKDF2 iterations to 1 for tests — default 600k iterations is CPU-intensive
            // and causes 60s+ timeouts on loaded CI machines (password sign-in late in the suite).
            services.Configure<PasswordHasherOptions>(opts => opts.IterationCount = 1);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _kestrelHost?.Dispose();
        }

        base.Dispose(disposing);
    }
}

internal sealed class NullAvatarService : IAvatarService
{
    public Task<Uri?> GetAvatarUrlAsync(string profileIdentifier, CancellationToken cancellationToken = default)
        => Task.FromResult<Uri?>(null);
}

#pragma warning disable S101
internal sealed class AlwaysPassCAPTCHAService : ICAPTCHAService
{
    public string? SiteKey => null;

    public decimal ScoreThreshold => 0.5m;

    public bool IsExempt(string? email) => false;

    public Task<decimal> VerifyAsync(string? token, CancellationToken cancellationToken = default)
        => Task.FromResult(1.0m);
}

internal sealed class TestServiceBusSenderFactory : IAzureClientFactory<ServiceBusSender>
{
    private readonly ServiceBusSender _sender;

    public TestServiceBusSenderFactory(ServiceBusSender sender) => _sender = sender;

    public ServiceBusSender CreateClient(string name) => _sender;
}
