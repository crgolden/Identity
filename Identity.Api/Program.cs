#pragma warning disable SA1200
using Identity.Extensions;
using Microsoft.AspNetCore.Cors.Infrastructure;
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
    await builder.AddObservabilityAsync(secretClient);
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
    await builder.AddAuthAsync(secretClient);
    await builder.AddPersistenceAsync(secretClient, healthChecksBuilder, identityBuilder, identityServerBuilder);
    builder.AddCors();
    await builder.AddEmailAsync(secretClient);
    await builder.AddPictureAsync(secretClient);
    builder.Services.AddRazorPages();

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
