#pragma warning disable SA1200
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Claims;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
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
using Microsoft.Extensions.Options;
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
    var dataProtectionBuilder = builder.Services.AddDataProtection();
    var sqlConnectionStringBuilderSection = builder.Configuration.GetSection(nameof(SqlConnectionStringBuilder));
    if (!sqlConnectionStringBuilderSection.Exists())
    {
        throw new InvalidOperationException($"Missing '{nameof(SqlConnectionStringBuilder)}' section.");
    }

    var sqlConnectionStringBuilder = sqlConnectionStringBuilderSection.Get<SqlConnectionStringBuilder>() ?? throw new InvalidOperationException($"Invalid '{nameof(SqlConnectionStringBuilder)}' section.");
    var corsPolicySection = builder.Configuration.GetSection(nameof(CorsPolicy));
    if (!corsPolicySection.Exists())
    {
        throw new InvalidOperationException($"Missing '{nameof(CorsPolicy)}' section.");
    }

    builder.Services.AddSerilog((sp, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(sp)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName());
    string googleClientId, googleClientSecret, resendApiToken, gravatarApiSecretKey;
    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddUserSecrets("aspnet-Identity-149346d0-999f-4a74-8ff7-2a92d39790f2");
        googleClientId = builder.Configuration.GetValue<string?>("GoogleClientId") ?? throw new InvalidOperationException("Invalid 'GoogleClientId'.");
        googleClientSecret = builder.Configuration.GetValue<string?>("GoogleClientSecret") ?? throw new InvalidOperationException("Invalid 'GoogleClientSecret'.");
        resendApiToken = builder.Configuration.GetValue<string?>("ResendApiToken") ?? throw new InvalidOperationException("Invalid 'ResendApiToken'.");
        gravatarApiSecretKey = builder.Configuration.GetValue<string?>("GravatarApiSecretKey") ?? throw new InvalidOperationException("Invalid 'GravatarApiSecretKey'.");
        builder.Services
            .Configure<IdentityPasskeyOptions>(identityPasskeyOptions =>
            {
                identityPasskeyOptions.ValidateOrigin = context => ValueTask.FromResult(context.Origin == "https://localhost:7261");
            })
            .AddDatabaseDeveloperPageExceptionFilter()
            .AddSerilog(loggerConfiguration =>
            {
                loggerConfiguration.Filter.ByExcluding(Matching.FromSource("Duende.IdentityServer.Diagnostics.Summary"));
            });
        dataProtectionBuilder.UseEphemeralDataProtectionProvider();
    }
    else
    {
        var keyVaultUrl = builder.Configuration.GetValue<Uri?>("KeyVaultUri") ??
                          throw new InvalidOperationException("Invalid 'KeyVaultUri'.");
        var defaultAzureCredentialOptionsSection = builder.Configuration.GetSection(nameof(DefaultAzureCredentialOptions));
        if (!defaultAzureCredentialOptionsSection.Exists())
        {
            throw new InvalidOperationException($"Missing '{nameof(DefaultAzureCredentialOptions)}' section.");
        }

        var defaultAzureCredentialOptions = defaultAzureCredentialOptionsSection.Get<DefaultAzureCredentialOptions?>() ?? throw new InvalidOperationException($"Invalid '{nameof(DefaultAzureCredentialOptions)}'.");
        var tokenCredential = new DefaultAzureCredential(defaultAzureCredentialOptions);
        var secrets = builder.GetSecrets(keyVaultUrl, tokenCredential);
        googleClientId = secrets.GoogleClientId.Value;
        googleClientSecret = secrets.GoogleClientSecret.Value;
        resendApiToken = secrets.ResendApiToken.Value;
        gravatarApiSecretKey = secrets.GravatarApiSecretKey.Value;
        sqlConnectionStringBuilder.UserID = secrets.SqlServerUserId.Value;
        sqlConnectionStringBuilder.Password = secrets.SqlServerPassword.Value;
        if (builder.Environment.IsProduction())
        {
            var applicationName = builder.Configuration.GetValue<string?>("WEBSITE_SITE_NAME") ?? throw new InvalidOperationException("Invalid 'WEBSITE_SITE_NAME'.");
            Uri blobUrl = builder.Configuration.GetValue<Uri?>("BlobUri") ?? throw new InvalidOperationException("Invalid 'BlobUri'."),
                dataProtectionKeyIdentifier = builder.Configuration.GetValue<Uri?>("DataProtectionKeyIdentifier") ?? throw new InvalidOperationException("Invalid 'DataProtectionKeyIdentifier'."),
                elasticsearchNode = builder.Configuration.GetValue<Uri?>("ElasticsearchNode") ?? throw new InvalidOperationException("Invalid 'ElasticsearchNode'.");
            dataProtectionBuilder.SetApplicationName(applicationName)
                .PersistKeysToAzureBlobStorage(blobUrl, tokenCredential)
                .ProtectKeysWithAzureKeyVault(dataProtectionKeyIdentifier, tokenCredential).Services
                .AddAzureClientsCore(true);
            builder.Logging.AddOpenTelemetry(openTelemetryLoggerOptions =>
            {
                openTelemetryLoggerOptions.IncludeFormattedMessage = true;
                openTelemetryLoggerOptions.IncludeScopes = true;
            });
            builder.Services
                .AddSerilog(loggerConfiguration => loggerConfiguration.WriteTo.Elasticsearch(
                    [elasticsearchNode],
                    elasticsearchSinkOptions =>
                    {
                        elasticsearchSinkOptions.DataStream = new DataStreamName("logs", "dotnet", nameof(Identity));
                        elasticsearchSinkOptions.BootstrapMethod = BootstrapMethod.Failure;
                    },
                    transportConfiguration =>
                    {
                        var elasticsearchUsername = secrets.ElasticsearchUsername.Value;
                        var elasticsearchPassword = secrets.ElasticsearchPassword.Value;
                        var header = new BasicAuthentication(elasticsearchUsername, elasticsearchPassword);
                        transportConfiguration.Authentication(header);
                    })
                    .Enrich.WithProperty(nameof(IHostEnvironment.ApplicationName), applicationName))
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
                        aspNetCoreTraceInstrumentationOptions.Filter = context =>
                        {
                            return !context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter())
                .UseAzureMonitor();
        }
        else
        {
            dataProtectionBuilder.UseEphemeralDataProtectionProvider();
            builder.Services.AddSerilog(loggerConfiguration =>
            {
                loggerConfiguration.Filter.ByExcluding(Matching.FromSource("Duende.IdentityServer.Diagnostics.Summary"));
            });
        }
    }

    builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();
    builder.Services
        .AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>(identityOptions =>
        {
            identityOptions.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            identityOptions.SignIn.RequireConfirmedAccount = true;
        })
        .AddDefaultUI()
        .AddDefaultTokenProviders()
        .AddEntityFrameworkStores<ApplicationDbContext>();
    builder.Services
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
        });
    builder.Services
        .AddAuthentication()
        .AddGoogleOpenIdConnect(
            GoogleOpenIdConnectDefaults.AuthenticationScheme,
            nameof(Google),
            openIdConnectOptions =>
            {
                openIdConnectOptions.SignInScheme = IdentityConstants.ExternalScheme;
                openIdConnectOptions.ClientId = googleClientId;
                openIdConnectOptions.ClientSecret = googleClientSecret;
            });
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
            .ConfigureWarnings(warningsConfigurationBuilder => warningsConfigurationBuilder.Throw(RelationalEventId.MultipleCollectionIncludeWarning)));
    builder.Services.Configure<CorsPolicy>(corsPolicySection).AddCors();
    builder.Services
        .Configure<ResendClientOptions>(configureOptions =>
        {
            configureOptions.ApiToken = resendApiToken;
        })
        .AddHttpClient<ResendClient>().Services
        .AddTransient<IResend, ResendClient>()
        .AddTransient<IEmailSender, EmailSender>();
    builder.Services
        .AddHttpClient<IGravatar, Gravatar>(httpClient =>
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.BearerScheme, gravatarApiSecretKey);
        }).Services
        .AddScoped<IAvatarService, GravatarService>();
    builder.Services
        .Configure<ForwardedHeadersOptions>(forwardedHeadersOptions =>
        {
            forwardedHeadersOptions.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            forwardedHeadersOptions.KnownIPNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();
        })
        .AddRazorPages();

    var app = builder.Build();
    app.UseForwardedHeaders();
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, _) =>
        {
            var activity = Activity.Current;
            if (activity is null)
            {
                return;
            }

            diagnosticContext.Set(nameof(Activity.TraceId), activity.TraceId.ToString());
            diagnosticContext.Set(nameof(Activity.SpanId), activity.SpanId.ToString());
        };
    });
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
    app.Use((ctx, next) =>
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
    app.MapAdditionalIdentityEndpoints();
    app.MapHealthChecks("/health").DisableHttpMetrics();
    app.MapStaticAssets();
    app.MapRazorPages()
       .WithStaticAssets()
       .RequireAuthorization();
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Serilog.Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Serilog.Log.CloseAndFlushAsync();
}
