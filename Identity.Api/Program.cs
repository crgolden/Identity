#pragma warning disable SA1200
using System.Diagnostics;
using System.Security.Claims;
using Identity.Extensions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Serilog;
#pragma warning restore SA1200

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddUserSecrets("aspnet-Identity-149346d0-999f-4a74-8ff7-2a92d39790f2");
    }

    var tokenCredential = await builder.Configuration.ToTokenCredentialAsync();
    var secretClient = builder.Configuration.ToSecretClient(tokenCredential);

    // Fetch all Key Vault secrets in one parallel batch to avoid sequential round trips.
    var secrets = await Task.WhenAll(
        secretClient.GetSecretAsync("ElasticsearchUsername"),
        secretClient.GetSecretAsync("ElasticsearchPassword"),
        secretClient.GetSecretAsync("SqlServerUserId"),
        secretClient.GetSecretAsync("SqlServerPassword"),
        secretClient.GetSecretAsync("GoogleClientId"),
        secretClient.GetSecretAsync("GoogleClientSecret"),
        secretClient.GetSecretAsync("ResendApiToken"),
        secretClient.GetSecretAsync("GravatarApiSecretKey"));

    builder.AddObservability(secrets[0].Value.Value, secrets[1].Value.Value);
    builder.AddDataProtection(tokenCredential);
    var healthChecksBuilder = builder.Services.AddHealthChecks();
    var identityBuilder = builder.Services
        .AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>(identityOptions =>
        {
            identityOptions.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            identityOptions.SignIn.RequireConfirmedAccount = true;
        })
        .AddDefaultUI()
        .AddDefaultTokenProviders();
    var identityServerBuilder = builder.Services
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
        .AddLicenseSummary();
    builder.AddAuth(secrets[4].Value.Value, secrets[5].Value.Value);
    builder.AddPersistence(secrets[2].Value.Value, secrets[3].Value.Value, healthChecksBuilder, identityBuilder, identityServerBuilder);
    builder.AddCors();
    builder.AddEmail(secrets[6].Value.Value);
    builder.AddPicture(secrets[7].Value.Value);
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
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
