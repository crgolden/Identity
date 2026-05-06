#pragma warning disable SA1200
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Claims;
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
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Azure;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Resend;
using Serilog;
using Serilog.Filters;
#pragma warning restore SA1200

Serilog.Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    string googleClientId, googleClientSecret, resendApiToken, gravatarApiSecretKey, reCAPTCHASiteKey, reCAPTCHASecretKey;
    var sqlConnectionStringBuilderSection = builder.Configuration.GetRequiredSection(nameof(SqlConnectionStringBuilder));
    var sqlConnectionStringBuilder = sqlConnectionStringBuilderSection.Get<SqlConnectionStringBuilder>() ?? throw new InvalidOperationException($"Invalid '{nameof(SqlConnectionStringBuilder)}' section.");
    var corsPolicySection = builder.Configuration.GetRequiredSection(nameof(CorsPolicy));
    var corsPolicy = corsPolicySection.Get<CorsPolicy>() ?? throw new InvalidOperationException($"Invalid '{nameof(CorsPolicy)}' section.");
    var recaptchaVerifyEndpoint = builder.Configuration.GetRequired<Uri>("RecaptchaVerifyEndpoint");
    if (builder.Environment.IsProduction())
    {
        var defaultAzureCredentialOptionsSection = builder.Configuration.GetRequiredSection(nameof(DefaultAzureCredentialOptions));
        var defaultAzureCredentialOptions = defaultAzureCredentialOptionsSection.Get<DefaultAzureCredentialOptions>() ?? throw new InvalidOperationException($"Invalid '{nameof(DefaultAzureCredentialOptions)}' section.");
        var tokenCredential = new DefaultAzureCredential(defaultAzureCredentialOptions);
        Uri blobUri = builder.Configuration.GetRequired<Uri>("BlobUri"),
            dataProtectionKeyIdentifier = builder.Configuration.GetRequired<Uri>("DataProtectionKeyIdentifier"),
            elasticsearchNode = builder.Configuration.GetRequired<Uri>("ElasticsearchNode"),
            keyVaultUrl = builder.Configuration.GetRequired<Uri>("KeyVaultUri");
        var applicationName = builder.Configuration.GetRequired<string>("WEBSITE_SITE_NAME");
        var secretClient = new SecretClient(keyVaultUrl, tokenCredential);
        var secrets = secretClient.GetIdentitySecrets();
        googleClientId = secrets.GoogleClientId.Value;
        googleClientSecret = secrets.GoogleClientSecret.Value;
        resendApiToken = secrets.ResendApiToken.Value;
        gravatarApiSecretKey = secrets.GravatarApiSecretKey.Value;
        reCAPTCHASiteKey = secrets.ReCAPTCHASiteKey.Value;
        reCAPTCHASecretKey = secrets.ReCAPTCHASecretKey.Value;
        sqlConnectionStringBuilder.UserID = secrets.SqlServerUserId.Value;
        sqlConnectionStringBuilder.Password = secrets.SqlServerPassword.Value;
        builder.Logging.AddOpenTelemetry(openTelemetryLoggerOptions =>
        {
            openTelemetryLoggerOptions.IncludeFormattedMessage = true;
            openTelemetryLoggerOptions.IncludeScopes = true;
        });
        builder.Services
            .AddSerilog((serviceProvider, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(serviceProvider)
                .Enrich.WithProperty(nameof(IHostEnvironment.ApplicationName), applicationName)
                .WriteTo.Elasticsearch(
                    [elasticsearchNode],
                    elasticsearchSinkOptions =>
                    {
                        elasticsearchSinkOptions.DataStream = new DataStreamName("logs", "dotnet", nameof(Identity));
                        elasticsearchSinkOptions.BootstrapMethod = BootstrapMethod.Failure;
                    },
                    transportConfiguration =>
                    {
                        var header = new BasicAuthentication(secrets.ElasticsearchUsername.Value, secrets.ElasticsearchPassword.Value);
                        transportConfiguration.Authentication(header);
                    }))
            .AddOpenTelemetry()
            .ConfigureResource(resourceBuilder => resourceBuilder
                .AddService(applicationName, null, typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0")
                .AddAttributes(new Dictionary<string, object>(1)
                {
                    ["deployment.environment"] = builder.Environment.EnvironmentName.ToLowerInvariant()
                }))
            .WithMetrics(meterProviderBuilder => meterProviderBuilder
                .AddMeter(Duende.IdentityServer.Telemetry.ServiceName)
                .AddMeter(nameof(Identity))
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracerProviderBuilder => tracerProviderBuilder
                .SetSampler(new AlwaysOnSampler())
                .AddSource(nameof(Identity))
                .AddSource(IdentityServerConstants.Tracing.Basic)
                .AddSource(IdentityServerConstants.Tracing.Cache)
                .AddSource(IdentityServerConstants.Tracing.Services)
                .AddSource(IdentityServerConstants.Tracing.Stores)
                .AddSource(IdentityServerConstants.Tracing.Validation)
                .AddAspNetCoreInstrumentation(aspNetCoreTraceInstrumentationOptions =>
                {
                    aspNetCoreTraceInstrumentationOptions.Filter = context => !context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
                })
                .AddHttpClientInstrumentation())
            .UseAzureMonitor().Services
            .AddDataProtection()
            .SetApplicationName(applicationName)
            .PersistKeysToAzureBlobStorage(blobUri, tokenCredential)
            .ProtectKeysWithAzureKeyVault(dataProtectionKeyIdentifier, tokenCredential).Services
            .AddAzureClientsCore(true);
    }
    else
    {
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddUserSecrets("aspnet-Identity-149346d0-999f-4a74-8ff7-2a92d39790f2");
            builder.Services
                .Configure<IdentityPasskeyOptions>(identityPasskeyOptions =>
                {
                    identityPasskeyOptions.ValidateOrigin = context => ValueTask.FromResult(context.Origin == "https://localhost:7261");
                })
                .AddDatabaseDeveloperPageExceptionFilter();
        }

        var secrets = builder.Configuration.GetIdentitySecrets();
        googleClientId = secrets.GoogleClientId;
        googleClientSecret = secrets.GoogleClientSecret;
        resendApiToken = secrets.ResendApiToken;
        gravatarApiSecretKey = secrets.GravatarApiSecretKey;
        reCAPTCHASiteKey = secrets.ReCAPTCHASiteKey;
        reCAPTCHASecretKey = secrets.ReCAPTCHASecretKey;
        builder.Services
            .AddSerilog((serviceProvider, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(serviceProvider)
                .Filter.ByExcluding(Matching.FromSource("Duende.IdentityServer.Diagnostics.Summary")))
            .AddDataProtection()
            .UseEphemeralDataProtectionProvider();
    }

    builder.Services
        .AddDbContextPool<ApplicationDbContext>(dbContextOptionsBuilder => dbContextOptionsBuilder
            .UseSqlServer(sqlConnectionStringBuilder.ConnectionString, sqlServerDbContextOptionsBuilder =>
            {
                var querySplittingBehavior = builder.Configuration.GetValue<QuerySplittingBehavior?>(nameof(QuerySplittingBehavior));
                if (querySplittingBehavior.HasValue)
                {
                    sqlServerDbContextOptionsBuilder.UseQuerySplittingBehavior(querySplittingBehavior.Value);
                }
            })
            .ConfigureWarnings(warningsConfigurationBuilder => warningsConfigurationBuilder.Throw(RelationalEventId.MultipleCollectionIncludeWarning)))
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
            identityServerOptions.UserInteraction.ConsentUrl = "/Account/Manage/Consent";
            identityServerOptions.UserInteraction.ErrorUrl = "/Error";
            identityServerOptions.UserInteraction.LoginUrl = "/Account/Login";
            identityServerOptions.UserInteraction.LogoutUrl = "/Account/Logout";
        })
        .AddAspNetIdentity<IdentityUser<Guid>>()
        .AddLicenseSummary()
        .AddConfigurationStore<ApplicationDbContext>(configurationStoreOptions =>
        {
            configurationStoreOptions.EnablePooling = true;
        })
        .AddOperationalStore<ApplicationDbContext>(operationalStoreOptions =>
        {
            operationalStoreOptions.EnablePooling = true;
        }).Services
        .AddAuthentication()
        .AddGoogleOpenIdConnect(
            GoogleOpenIdConnectDefaults.AuthenticationScheme,
            nameof(Google),
            openIdConnectOptions =>
            {
                openIdConnectOptions.SignInScheme = IdentityConstants.ExternalScheme;
                openIdConnectOptions.ClientId = googleClientId;
                openIdConnectOptions.ClientSecret = googleClientSecret;
            }).Services
        .Configure<ResendClientOptions>(configureOptions =>
        {
            configureOptions.ApiToken = resendApiToken;
        })
        .AddHttpClient<ResendClient>().Services
        .AddTransient<IResend, ResendClient>()
        .AddTransient<IEmailSender, EmailSender>()
        .AddHttpClient<IGravatar, Gravatar>(httpClient =>
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.BearerScheme, gravatarApiSecretKey);
        }).Services
        .AddScoped<IAvatarService, GravatarService>()
        .Configure<ReCAPTCHAOptions>(recaptchaOptions =>
        {
            recaptchaOptions.SiteKey = reCAPTCHASiteKey;
            recaptchaOptions.SecretKey = reCAPTCHASecretKey;
            recaptchaOptions.VerifyEndpoint = recaptchaVerifyEndpoint;
        })
        .AddHttpClient<ICAPTCHAService, ReCAPTCHAService>().Services
        .AddCors()
        .Configure<ForwardedHeadersOptions>(forwardedHeadersOptions =>
        {
            forwardedHeadersOptions.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            forwardedHeadersOptions.KnownIPNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();
        })
        .AddRazorPages().Services
        .AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>();

    var webApplication = builder.Build();
    webApplication
        .UseForwardedHeaders()
        .UseSerilogRequestLogging(requestLoggingOptions =>
        {
            requestLoggingOptions.EnrichDiagnosticContext = (diagnosticContext, _) =>
            {
                if (Activity.Current is null)
                {
                    return;
                }

                diagnosticContext.Set(nameof(Activity.TraceId), Activity.Current.TraceId.ToString());
                diagnosticContext.Set(nameof(Activity.SpanId), Activity.Current.SpanId.ToString());
            };
        });

    if (webApplication.Environment.IsDevelopment())
    {
        webApplication.UseDeveloperExceptionPage();
    }
    else
    {
        webApplication.UseExceptionHandler("/Error").UseHsts();
    }

    webApplication
        .UseHttpsRedirection()
        .UseRouting()
        .UseIdentityServer()
        .UseCors(corsPolicyBuilder =>
        {
            corsPolicyBuilder.WithOrigins(corsPolicy.Origins.ToArray());
        })
        .UseAuthorization()
        .Use((ctx, next) =>
        {
            if (ctx.User.Identity?.IsAuthenticated != true)
            {
                return next(ctx);
            }

            using (Serilog.Context.LogContext.PushProperty("UserId", ctx.User.FindFirstValue("sub")))
            using (Serilog.Context.LogContext.PushProperty("UserEmail", ctx.User.FindFirstValue("email")))
            {
                return next(ctx);
            }
        });
    webApplication.MapAdditionalIdentityEndpoints();
    webApplication.MapHealthChecks("/health").DisableHttpMetrics();
    webApplication.MapStaticAssets();
    webApplication.MapRazorPages().WithStaticAssets().RequireAuthorization();
    await webApplication.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Serilog.Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Serilog.Log.CloseAndFlushAsync();
}
