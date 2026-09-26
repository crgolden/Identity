namespace Identity.Tests.E2E.Infrastructure;

using System.Globalization;
using System.Net;
using Azure.Messaging.ServiceBus;
using Identity.Avatar;
using Identity.CAPTCHA;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class IdentityWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly string NeverDialledServiceBusConnectionString = BuildNeverDialledServiceBusConnectionString();

    private IHost? _kestrelHost;
    private string? _serverAddress;

    public EmailCaptureSender EmailCapture { get; } = new();

    public string? RefusedCatalog { get; private set; }

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
        builder.UseStaticWebAssets();

        builder.ConfigureAppConfiguration(configBuilder =>
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ServiceBusNames.ConnectionStringSettingKey] = NeverDialledServiceBusConnectionString
            }));

        builder.ConfigureServices((context, services) =>
        {
            var catalog = context.Configuration[$"{nameof(SqlConnectionStringBuilder)}:{nameof(SqlConnectionStringBuilder.InitialCatalog)}"];
            var testCatalogSuffix = E2ESettings.Read(context.Configuration).TestCatalogSuffix;
            if (catalog is null || !catalog.EndsWith(testCatalogSuffix, StringComparison.Ordinal))
            {
                RefusedCatalog = catalog;
                throw new InvalidOperationException(
                    $"The E2E tier writes to the catalog it is given, so it refuses '{catalog}': the catalog must end in '{testCatalogSuffix}'.");
            }

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
            services.AddSingleton<ICAPTCHAService, AlwaysPassCAPTCHAService>();

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

    private static string BuildNeverDialledServiceBusConnectionString()
    {
        var namespaceName = Guid.NewGuid().ToString("N");
        var sharedAccessKeyName = Guid.NewGuid().ToString("N");
        var sharedAccessKeyBytes = Guid.NewGuid().ToByteArray();
        var sharedAccessKey = Convert.ToBase64String(sharedAccessKeyBytes);
        return string.Format(
            CultureInfo.InvariantCulture,
            ServiceBusConstants.ConnectionStringFormat,
            namespaceName,
            sharedAccessKeyName,
            sharedAccessKey);
    }
}
