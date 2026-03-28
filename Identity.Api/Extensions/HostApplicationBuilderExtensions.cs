namespace Identity.Extensions;

using System.Net.Http.Headers;
using Azure.Core;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Security.KeyVault.Secrets;
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
        public async Task<IHostApplicationBuilder> AddObservabilityAsync(SecretClient secretClient, CancellationToken cancellationToken = default)
        {
            var elasticsearchNode = builder.Configuration.GetValue<Uri>("ElasticsearchNode") ?? throw new InvalidOperationException("Invalid 'ElasticsearchNode'.");
            var tasks = new[]
            {
                secretClient.GetSecretAsync("ElasticsearchUsername", cancellationToken: cancellationToken),
                secretClient.GetSecretAsync("ElasticsearchPassword", cancellationToken: cancellationToken),
            };
            var result = await Task.WhenAll(tasks);
            builder.Logging.AddOpenTelemetry(openTelemetryLoggerOptions =>
            {
                openTelemetryLoggerOptions.IncludeFormattedMessage = true;
                openTelemetryLoggerOptions.IncludeScopes = true;
            });
            builder.Services
                .AddOpenTelemetry()
                .ConfigureResource(x =>
                {
                    x.AddService(
                        serviceName: builder.Environment.ApplicationName,
                        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0");
                    x.AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = builder.Environment.EnvironmentName.ToLowerInvariant()
                    });
                })
                .UseAzureMonitor()
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
                            .WriteTo.Elasticsearch(
                                [elasticsearchNode],
                                elasticsearchSinkOptions =>
                                {
                                    elasticsearchSinkOptions.DataStream = new DataStreamName("logs", "dotnet", nameof(Identity));
                                    elasticsearchSinkOptions.BootstrapMethod = BootstrapMethod.Failure;
                                },
                                transportConfiguration =>
                                {
                                    var header = new BasicAuthentication(result[0].Value.Value, result[1].Value.Value);
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
            var blobUrl = builder.Configuration.GetValue<Uri>("BlobUri") ?? throw new InvalidOperationException("Invalid 'BlobUri'.");
            var dataProtectionKeyIdentifier = builder.Configuration.GetValue<Uri>("DataProtectionKeyIdentifier") ?? throw new InvalidOperationException("Invalid 'DataProtectionKeyIdentifier'.");
            builder.Services
                .AddDataProtection()
                .SetApplicationName(builder.Environment.ApplicationName)
                .PersistKeysToAzureBlobStorage(blobUrl, tokenCredential)
                .ProtectKeysWithAzureKeyVault(dataProtectionKeyIdentifier, tokenCredential).Services
                .AddAzureClientsCore(true);
            return builder;
        }

        public async Task<IHostApplicationBuilder> AddPersistenceAsync(
            SecretClient secretClient,
            IHealthChecksBuilder healthChecksBuilder,
            IdentityBuilder identityBuilder,
            IIdentityServerBuilder identityServerBuilder,
            CancellationToken cancellationToken = default)
        {
            var sqlConnectionStringBuilder = builder.Configuration
                .GetSection(nameof(SqlConnectionStringBuilder))
                .Get<SqlConnectionStringBuilder>() ?? throw new InvalidOperationException($"Invalid '{nameof(SqlConnectionStringBuilder)}' section.");
            var tasks = new[]
            {
                secretClient.GetSecretAsync("SqlServerUserId", cancellationToken: cancellationToken),
                secretClient.GetSecretAsync("SqlServerPassword", cancellationToken: cancellationToken)
            };
            var result = await Task.WhenAll(tasks);
            builder.Services
                    .AddDbContextPool<ApplicationDbContext>((sp, dbContextOptionsBuilder) =>
                    {
                        if (!sqlConnectionStringBuilder.IntegratedSecurity)
                        {
                            sqlConnectionStringBuilder.UserID = result[0].Value.Value;
                            sqlConnectionStringBuilder.Password = result[1].Value.Value;
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

        public async Task<IHostApplicationBuilder> AddAuthAsync(SecretClient secretClient, CancellationToken cancellationToken = default)
        {
            var tasks = new[]
            {
                secretClient.GetSecretAsync("GoogleClientId", cancellationToken: cancellationToken),
                secretClient.GetSecretAsync("GoogleClientSecret", cancellationToken: cancellationToken)
            };
            var result = await Task.WhenAll(tasks);
            builder.Services
                .AddAuthentication()
                .AddGoogleOpenIdConnect(
                    authenticationScheme: GoogleOpenIdConnectDefaults.AuthenticationScheme,
                    displayName: nameof(Google),
                    configureOptions: openIdConnectOptions =>
                    {
                        openIdConnectOptions.SignInScheme = IdentityConstants.ExternalScheme;
                        openIdConnectOptions.ClientId = result[0].Value.Value;
                        openIdConnectOptions.ClientSecret = result[1].Value.Value;
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

        public async Task<IHostApplicationBuilder> AddEmailAsync(SecretClient secretClient, CancellationToken cancellationToken = default)
        {
            var result = await secretClient.GetSecretAsync("ResendApiToken", cancellationToken: cancellationToken);
            builder.Services
                .Configure<ResendClientOptions>(configureOptions =>
                {
                    configureOptions.ApiToken = result.Value.Value;
                })
                .AddHttpClient<ResendClient>().Services
                .AddTransient<IResend, ResendClient>()
                .AddTransient<IEmailSender, EmailSender>();
            return builder;
        }

        public async Task<IHostApplicationBuilder> AddPictureAsync(SecretClient secretClient, CancellationToken cancellationToken = default)
        {
            var result = await secretClient.GetSecretAsync("GravatarApiSecretKey", cancellationToken: cancellationToken);
            builder.Services
                .AddHttpClient<IGravatar, Gravatar>((sp, httpClient) =>
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.BearerScheme, result.Value.Value);
                }).Services
                .AddScoped<IAvatarService, GravatarService>();
            return builder;
        }
    }
}
