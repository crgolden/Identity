#pragma warning disable SA1200
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Security.KeyVault.Secrets;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;
using Google.Apis.Auth.AspNetCore3;
using Identity;
using Identity.Extensions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Resend;
using Serilog;
#pragma warning restore SA1200

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var (corsPolicySection, sqlConnectionStringBuilderSection, defaultAzureCredentialOptionsSection) = builder.Configuration.GetSections();
    var defaultAzureCredentialOptions = defaultAzureCredentialOptionsSection.Get<DefaultAzureCredentialOptions>() ?? throw new InvalidOperationException($"Invalid '{nameof(DefaultAzureCredentialOptions)}'");
    TokenCredential tokenCredential = new DefaultAzureCredential(defaultAzureCredentialOptions);
    var (elasticsearchNode, keyVaultUrl, blobUrl, dataProtectionKeyIdentifier) = builder.Configuration.GetUris();
    var secretClient = new SecretClient(keyVaultUrl, tokenCredential);
    var (gravatarApiKeySecret, elasticsearchUsername, elasticsearchPassword, sqlServerUserId, sqlServerPassword, googleClientId, googleClientSecret, resendApiToken) = await secretClient.GetSecrets();
    builder.Services
        .AddOpenTelemetry()
        .UseAzureMonitor()
        .WithMetrics(meterProviderBuilder =>
        {
            meterProviderBuilder
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation();
        })
        .WithTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation(aspNetCoreTraceInstrumentationOptions =>
                {
                    aspNetCoreTraceInstrumentationOptions.Filter = context =>
                    {
                        return !context.Request.Path.StartsWithSegments("/Health");
                    };
                })
                .AddHttpClientInstrumentation();
        }).Services
        .AddSerilog((sp, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(sp)
            .Enrich.FromLogContext()
            .WriteTo.Elasticsearch(
                [elasticsearchNode],
                elasticsearchSinkOptions =>
                {
                    elasticsearchSinkOptions.DataStream = new DataStreamName("logs", "dotnet", "identity");
                    elasticsearchSinkOptions.BootstrapMethod = BootstrapMethod.Failure;
                },
                transportConfiguration =>
                {
                    var header = new BasicAuthentication(elasticsearchUsername.Value, elasticsearchPassword.Value);
                    transportConfiguration.Authentication(header);
                }))
        .Configure<SqlConnectionStringBuilder>(sqlConnectionStringBuilderSection)
        .AddDbContext<ApplicationDbContext>((sp, dbContextOptionsBuilder) =>
        {
            var sqlConnectionStringBuilder = sp.GetRequiredService<IOptions<SqlConnectionStringBuilder>>().Value;
            if (!sqlConnectionStringBuilder.IntegratedSecurity)
            {
                sqlConnectionStringBuilder.UserID = sqlServerUserId.Value;
                sqlConnectionStringBuilder.Password = sqlServerPassword.Value;
            }

            dbContextOptionsBuilder.UseSqlServer(sqlConnectionStringBuilder.ConnectionString);
        })
        .AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>(identityOptions =>
        {
            identityOptions.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            identityOptions.SignIn.RequireConfirmedAccount = true;
        })
        .AddDefaultUI()
        .AddDefaultTokenProviders()
        .AddEntityFrameworkStores<ApplicationDbContext>().Services
        .AddIdentityServer(identityServerOptions =>
        {
            identityServerOptions.Events.RaiseErrorEvents = true;
            identityServerOptions.Events.RaiseInformationEvents = true;
            identityServerOptions.Events.RaiseFailureEvents = true;
            identityServerOptions.Events.RaiseSuccessEvents = true;
            identityServerOptions.UserInteraction.ErrorUrl = "/Error";
        })
        .AddConfigurationStore<ApplicationDbContext>()
        .AddOperationalStore<ApplicationDbContext>()
        .AddAspNetIdentity<IdentityUser<Guid>>()
        .AddLicenseSummary().Services
        .AddAuthentication()
        .AddGoogleOpenIdConnect(
            authenticationScheme: GoogleOpenIdConnectDefaults.AuthenticationScheme,
            displayName: nameof(Google),
            configureOptions: openIdConnectOptions =>
            {
                openIdConnectOptions.SignInScheme = IdentityConstants.ExternalScheme;
                openIdConnectOptions.ClientId = googleClientId.Value;
                openIdConnectOptions.ClientSecret = googleClientSecret.Value;
            }).Services
        .Configure<ResendClientOptions>(configureOptions =>
        {
            configureOptions.ApiToken = resendApiToken.Value;
        })
        .AddHttpClient<ResendClient>().Services
        .AddTransient<IResend, ResendClient>()
        .AddTransient<IEmailSender, EmailSender>()
        .AddHttpClient<IGravatar, Gravatar>((sp, httpClient) =>
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.BearerScheme, gravatarApiKeySecret.Value);
        }).Services
        .AddScoped<IAvatarService, GravatarService>()
        .AddRazorPages().Services
        .Configure<CorsPolicy>(corsPolicySection)
        .AddCors()
        .AddHealthChecks().AddDbContextCheck<ApplicationDbContext>().Services
        .AddDataProtection()
        .SetApplicationName(builder.Environment.ApplicationName)
        .PersistKeysToAzureBlobStorage(blobUrl, tokenCredential)
        .ProtectKeysWithAzureKeyVault(dataProtectionKeyIdentifier, tokenCredential).Services
        .AddAzureClientsCore(true);
    builder.Logging.AddOpenTelemetry(openTelemetryLoggerOptions =>
    {
        openTelemetryLoggerOptions.IncludeFormattedMessage = true;
        openTelemetryLoggerOptions.IncludeScopes = true;
    });
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        builder.Services.Configure<IdentityPasskeyOptions>(identityPasskeyOptions =>
        {
            identityPasskeyOptions.ValidateOrigin = context => ValueTask.FromResult(context.Origin == "https://localhost:7261");
        });
    }

    var app = builder.Build();
    app.UseSerilogRequestLogging();
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage().UseMigrationsEndPoint();
    }
    else
    {
        app.UseExceptionHandler("/Error").UseHsts();
    }

    app.UseHttpsRedirection()
        .UseRouting()
        .UseIdentityServer()
        .UseCors(corsPolicyBuilder =>
        {
            var corsPolicy = app.Services.GetRequiredService<IOptions<CorsPolicy>>().Value;
            corsPolicyBuilder.WithOrigins(corsPolicy.Origins.ToArray());
        })
        .UseAuthorization();
    app.MapAdditionalIdentityEndpoints();
    app.MapHealthChecks("Health");
    app.MapStaticAssets();
    app.MapRazorPages()
       .WithStaticAssets()
       .RequireAuthorization();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
