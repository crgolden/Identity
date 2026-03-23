#pragma warning disable SA1200
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Security.KeyVault.Secrets;
using Duende.IdentityServer;
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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Resend;
using Serilog;
using Serilog.Filters;
#pragma warning restore SA1200

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddUserSecrets("aspnet-Identity-149346d0-999f-4a74-8ff7-2a92d39790f2");
    }

    var (corsPolicySection, sqlConnectionStringBuilderSection, defaultAzureCredentialOptionsSection) = builder.Configuration.GetSections();
    var defaultAzureCredentialOptions = defaultAzureCredentialOptionsSection.Get<DefaultAzureCredentialOptions>() ?? throw new InvalidOperationException($"Invalid '{nameof(DefaultAzureCredentialOptions)}' section.");
    TokenCredential tokenCredential = new DefaultAzureCredential(defaultAzureCredentialOptions);
    var (elasticsearchNode, keyVaultUrl, blobUrl, dataProtectionKeyIdentifier) = builder.Configuration.GetUris();
    var secretClient = new SecretClient(keyVaultUrl, tokenCredential);
    var (gravatarApiKeySecret, elasticsearchUsername, elasticsearchPassword, sqlServerUserId, sqlServerPassword, googleClientId, googleClientSecret, resendApiToken) = await secretClient.GetSecrets();
    builder.Services
        .AddOpenTelemetry()
        .ConfigureResource(x => x.AddService(builder.Environment.ApplicationName))
        .UseAzureMonitor()
        .WithMetrics(meterProviderBuilder =>
        {
            meterProviderBuilder
                .AddMeter(Duende.IdentityServer.Telemetry.ServiceName)
                .AddMeter(Identity.Telemetry.ServiceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation();
        })
        .WithTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .AddSource(builder.Environment.ApplicationName)
                .AddSource(IdentityServerConstants.Tracing.Basic)
                .AddSource(IdentityServerConstants.Tracing.Cache)
                .AddSource(IdentityServerConstants.Tracing.Services)
                .AddSource(IdentityServerConstants.Tracing.Stores)
                .AddSource(IdentityServerConstants.Tracing.Validation)
                .AddAspNetCoreInstrumentation(aspNetCoreTraceInstrumentationOptions =>
                {
                    aspNetCoreTraceInstrumentationOptions.Filter = context =>
                    {
                        return !context.Request.Path.StartsWithSegments("/Health");
                    };
                })
                .AddHttpClientInstrumentation();
            if (builder.Environment.IsDevelopment())
            {
                tracerProviderBuilder.AddConsoleExporter();
            }
        }).Services
        .AddSerilog((sp, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(sp)
                .Enrich.FromLogContext();
            if (builder.Environment.IsProduction())
            {
                loggerConfiguration
                    .WriteTo.OpenTelemetry()
                    .WriteTo.Elasticsearch(
                        [elasticsearchNode],
                        elasticsearchSinkOptions =>
                        {
                            elasticsearchSinkOptions.DataStream = new DataStreamName("logs", "dotnet", nameof(Identity));
                            elasticsearchSinkOptions.BootstrapMethod = BootstrapMethod.Failure;
                        },
                        transportConfiguration =>
                        {
                            var header = new BasicAuthentication(elasticsearchUsername.Value, elasticsearchPassword.Value);
                            transportConfiguration.Authentication(header);
                        });
            }
            else
            {
                loggerConfiguration.Filter.ByExcluding(Matching.FromSource("Duende.IdentityServer.Diagnostics.Summary"));
            }
        })
        .Configure<SqlConnectionStringBuilder>(sqlConnectionStringBuilderSection)
        .AddDbContextPool<ApplicationDbContext>((sp, dbContextOptionsBuilder) =>
        {
            var sqlConnectionStringBuilder = sp.GetRequiredService<IOptions<SqlConnectionStringBuilder>>().Value;
            if (!sqlConnectionStringBuilder.IntegratedSecurity)
            {
                sqlConnectionStringBuilder.UserID = sqlServerUserId.Value;
                sqlConnectionStringBuilder.Password = sqlServerPassword.Value;
            }

            dbContextOptionsBuilder
                .UseSqlServer(sqlConnectionStringBuilder.ConnectionString, sqlServerDbContextOptionsBuilder =>
                {
                    var querySplittingBehavior = builder.Configuration.GetValue<QuerySplittingBehavior?>(nameof(QuerySplittingBehavior));
                    if (querySplittingBehavior.HasValue)
                    {
                        sqlServerDbContextOptionsBuilder.UseQuerySplittingBehavior(querySplittingBehavior.Value);
                    }
                })
                .ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
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
            if (builder.Environment.IsDevelopment())
            {
                identityServerOptions.Events.RaiseErrorEvents = true;
                identityServerOptions.Events.RaiseInformationEvents = true;
                identityServerOptions.Events.RaiseFailureEvents = true;
                identityServerOptions.Events.RaiseSuccessEvents = true;
            }

            identityServerOptions.UserInteraction.ConsentUrl = "/Consent/Index";
            identityServerOptions.UserInteraction.ErrorUrl = "/Error";
            identityServerOptions.UserInteraction.LoginUrl = "/Account/Login";
            identityServerOptions.UserInteraction.LogoutUrl = "/Account/Logout";
        })
        .AddConfigurationStore<ApplicationDbContext>(opt =>
        {
            opt.EnablePooling = true;
        })
        .AddOperationalStore<ApplicationDbContext>(opt =>
        {
            opt.EnablePooling = true;
        })
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
        app.UseDeveloperExceptionPage();
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
    app.MapHealthChecks("Health").DisableHttpMetrics();
    app.MapStaticAssets();
    app.MapRazorPages()
       .WithStaticAssets()
       .RequireAuthorization();
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
