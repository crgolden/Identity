namespace Identity.Extensions;

using System.Net.Http.Headers;
using Azure.Core;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Duende.IdentityServer;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;
using Google.Apis.Auth.AspNetCore3;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
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

public static class HostApplicationBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddObservability(string elasticsearchUsername, string elasticsearchPassword)
        {
            var applicationName = builder.Configuration["WEBSITE_SITE_NAME"];
            var elasticsearchNode = builder.Configuration.GetValue<Uri?>("ElasticsearchNode") ?? throw new InvalidOperationException("Invalid 'ElasticsearchNode'.");
            builder.Logging.AddOpenTelemetry(openTelemetryLoggerOptions =>
            {
                openTelemetryLoggerOptions.IncludeFormattedMessage = true;
                openTelemetryLoggerOptions.IncludeScopes = true;
            });
            var otelBuilder = builder.Services
                .AddOpenTelemetry()
                .ConfigureResource(resourceBuilder =>
                {
                    var serviceName = applicationName ?? builder.Environment.ApplicationName;
                    resourceBuilder.AddService(
                        serviceName: serviceName,
                        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0");
                    resourceBuilder.AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = builder.Environment.EnvironmentName.ToLowerInvariant()
                    });
                })
                .WithMetrics(meterProviderBuilder =>
                {
                    meterProviderBuilder
                        .AddMeter(Telemetry.ServiceName)
                        .AddMeter(nameof(Identity))
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation();
                })
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder
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
                        .AddHttpClientInstrumentation();
                    if (builder.Environment.IsDevelopment())
                    {
                        tracerProviderBuilder.AddConsoleExporter();
                    }
                });

            if (builder.Environment.IsProduction())
            {
                otelBuilder.UseAzureMonitor();
            }

            builder.Services.AddSerilog((sp, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(sp)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithEnvironmentName();
                if (!IsNullOrWhiteSpace(applicationName))
                {
                    loggerConfiguration
                        .Enrich.WithProperty(nameof(IHostEnvironment.ApplicationName), applicationName);
                }

                if (builder.Environment.IsProduction())
                {
                    loggerConfiguration
                        .WriteTo.Elasticsearch(
                            [elasticsearchNode],
                            elasticsearchSinkOptions =>
                            {
                                elasticsearchSinkOptions.DataStream = new DataStreamName("logs", "dotnet", nameof(Identity));
                                elasticsearchSinkOptions.BootstrapMethod = BootstrapMethod.Failure;
                            },
                            transportConfiguration =>
                            {
                                var header = new BasicAuthentication(elasticsearchUsername, elasticsearchPassword);
                                transportConfiguration.Authentication(header);
                            });
                }
                else
                {
                    loggerConfiguration.Filter.ByExcluding(Matching.FromSource("Duende.IdentityServer.Diagnostics.Summary"));
                }
            });
            return builder;
        }

        public IHostApplicationBuilder AddDataProtection(TokenCredential tokenCredential)
        {
            var blobUrl = builder.Configuration.GetValue<Uri?>("BlobUri") ?? throw new InvalidOperationException("Invalid 'BlobUri'.");
            var dataProtectionKeyIdentifier = builder.Configuration.GetValue<Uri?>("DataProtectionKeyIdentifier") ?? throw new InvalidOperationException("Invalid 'DataProtectionKeyIdentifier'.");
            builder.Services
                .AddDataProtection()
                .SetApplicationName(builder.Environment.ApplicationName)
                .PersistKeysToAzureBlobStorage(blobUrl, tokenCredential)
                .ProtectKeysWithAzureKeyVault(dataProtectionKeyIdentifier, tokenCredential).Services
                .AddAzureClientsCore(true);
            return builder;
        }

        public IHostApplicationBuilder AddPersistence(
            string sqlUserId,
            string sqlPassword,
            IHealthChecksBuilder healthChecksBuilder,
            IdentityBuilder identityBuilder,
            IIdentityServerBuilder identityServerBuilder)
        {
            var sqlConnectionStringBuilder = builder.Configuration
                .GetSection(nameof(SqlConnectionStringBuilder))
                .Get<SqlConnectionStringBuilder>() ?? throw new InvalidOperationException($"Invalid '{nameof(SqlConnectionStringBuilder)}' section.");
            builder.Services
                    .AddDbContextPool<ApplicationDbContext>((sp, dbContextOptionsBuilder) =>
                    {
                        if (!sqlConnectionStringBuilder.IntegratedSecurity)
                        {
                            sqlConnectionStringBuilder.UserID = sqlUserId;
                            sqlConnectionStringBuilder.Password = sqlPassword;
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
                    });
            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddDatabaseDeveloperPageExceptionFilter();
            }

            healthChecksBuilder.AddDbContextCheck<ApplicationDbContext>();
            identityBuilder.AddEntityFrameworkStores<ApplicationDbContext>();
            identityServerBuilder.
                AddConfigurationStore<ApplicationDbContext>(configurationStoreOptions =>
                {
                    configurationStoreOptions.EnablePooling = true;
                })
                .AddOperationalStore<ApplicationDbContext>(operationalStoreOptions =>
                {
                    operationalStoreOptions.EnablePooling = true;
                });
            return builder;
        }

        public IHostApplicationBuilder AddAuth(string googleClientId, string googleClientSecret)
        {
            builder.Services
                .AddAuthentication()
                .AddGoogleOpenIdConnect(
                    authenticationScheme: GoogleOpenIdConnectDefaults.AuthenticationScheme,
                    displayName: nameof(Google),
                    configureOptions: openIdConnectOptions =>
                    {
                        openIdConnectOptions.SignInScheme = IdentityConstants.ExternalScheme;
                        openIdConnectOptions.ClientId = googleClientId;
                        openIdConnectOptions.ClientSecret = googleClientSecret;
                    });
            if (builder.Environment.IsDevelopment())
            {
                builder.Services.Configure<IdentityPasskeyOptions>(identityPasskeyOptions =>
                {
                    identityPasskeyOptions.ValidateOrigin = context => ValueTask.FromResult(context.Origin == "https://localhost:7261");
                });
            }

            return builder;
        }

        public IHostApplicationBuilder AddCors()
        {
            var corsPolicySection = builder.Configuration.GetSection(nameof(CorsPolicy));
            if (!corsPolicySection.Exists())
            {
                throw new InvalidOperationException($"Missing '{nameof(CorsPolicy)}' section.");
            }

            builder.Services.Configure<CorsPolicy>(corsPolicySection).AddCors();
            return builder;
        }

        public IHostApplicationBuilder AddEmail(string resendApiToken)
        {
            builder.Services
                .Configure<ResendClientOptions>(configureOptions =>
                {
                    configureOptions.ApiToken = resendApiToken;
                })
                .AddHttpClient<ResendClient>().Services
                .AddTransient<IResend, ResendClient>()
                .AddTransient<IEmailSender, EmailSender>();
            return builder;
        }

        public IHostApplicationBuilder AddPicture(string gravatarApiSecretKey)
        {
            builder.Services
                .AddHttpClient<IGravatar, Gravatar>((sp, httpClient) =>
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.BearerScheme, gravatarApiSecretKey);
                }).Services
                .AddScoped<IAvatarService, GravatarService>();
            return builder;
        }
    }
}
