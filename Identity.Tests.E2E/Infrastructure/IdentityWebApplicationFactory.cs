namespace Identity.Tests.E2E.Infrastructure;

using System.Net;
using Azure.Messaging.ServiceBus;
using Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        var testHost = builder.Build();
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
            services.Configure<HostOptions>(opts =>
                opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

            if (!context.HostingEnvironment.IsProduction())
            {
                services.RemoveAll<ILoggerFactory>();
                services.AddLogging(lb => lb.AddConsole());
            }

            services.RemoveAll<IAzureClientFactory<ServiceBusClient>>();
            services.AddSingleton(EmailCapture);
            services.AddSingleton<IAzureClientFactory<ServiceBusClient>>(new TestServiceBusClientFactory(EmailCapture));

            services.RemoveAll<IAvatarService>();
            services.AddSingleton<IAvatarService>(new NullAvatarService());

            services.RemoveAll<ICAPTCHAService>();
            services.AddSingleton<ICAPTCHAService>(new AlwaysPassCAPTCHAService());

            services.Configure<PasswordHasherOptions>(opts => opts.IterationCount = 1);

            services.Replace(ServiceDescriptor.Singleton<IAuthenticationSchemeProvider, FakeGoogleSchemeProvider>());
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

internal sealed class TestServiceBusClientFactory : IAzureClientFactory<ServiceBusClient>
{
    private readonly TestServiceBusClient _client;

    public TestServiceBusClientFactory(ServiceBusSender sender) => _client = new TestServiceBusClient(sender);

    public ServiceBusClient CreateClient(string name) => _client;
}

internal sealed class TestServiceBusClient : ServiceBusClient
{
    private readonly ServiceBusSender _sender;

    public TestServiceBusClient(ServiceBusSender sender) => _sender = sender;

    public override ServiceBusSender CreateSender(string queueOrTopicName) => _sender;
}