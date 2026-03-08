using Azure.Security.KeyVault.Secrets;
using Google.Apis.Auth.AspNetCore3;
using Identity;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using Resend;

var builder = WebApplication.CreateBuilder(args);
var keyVaultSection = builder.Configuration.GetSection(nameof(Azure.Security.KeyVault));
var corsPolicySection = builder.Configuration.GetSection(nameof(CorsPolicy));
var sqlConnectionStringBuilderSection = builder.Configuration.GetSection(nameof(SqlConnectionStringBuilder));

builder.Services
    .Configure<SqlConnectionStringBuilder>(sqlConnectionStringBuilderSection)
    .AddDbContext<ApplicationDbContext>((sp, dbContextOptionsBuilder) =>
    {
        var sqlConnectionStringBuilder = sp.GetRequiredService<IOptions<SqlConnectionStringBuilder>>().Value;
        if (!sqlConnectionStringBuilder.IntegratedSecurity)
        {
            var secretClient = sp.GetRequiredService<SecretClient>();
            var sqlServerUserId = secretClient.GetSecret("SqlServerUserId");
            var sqlServerPassword = secretClient.GetSecret("SqlServerPassword");
            sqlConnectionStringBuilder.UserID = sqlServerUserId.Value.Value;
            sqlConnectionStringBuilder.Password = sqlServerPassword.Value.Value;
        }

        dbContextOptionsBuilder.UseSqlServer(sqlConnectionStringBuilder.ConnectionString, sqlServerDbContextOptionsBuilder =>
        {
            var assembly = typeof(ApplicationDbContext).Assembly;
            sqlServerDbContextOptionsBuilder.MigrationsAssembly(assembly);
        });
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
    .AddConfigurationStore<ApplicationDbContext>(configurationStoreOptions => { })
    .AddOperationalStore<ApplicationDbContext>(operationalStoreOptions => { })
    .AddAspNetIdentity<IdentityUser<Guid>>()
    .AddLicenseSummary().Services
    .AddAuthentication()
    .AddGoogleOpenIdConnect(
        authenticationScheme: GoogleOpenIdConnectDefaults.AuthenticationScheme,
        displayName: nameof(Google),
        configureOptions: openIdConnectOptions =>
        {
            openIdConnectOptions.SignInScheme = IdentityConstants.ExternalScheme;
        }).Services
    .AddTransient<IPostConfigureOptions<OpenIdConnectOptions>, OpenIdConnectConfigureOptions>()
    .AddOptions<ResendClientOptions>().Services
    .AddTransient<IPostConfigureOptions<ResendClientOptions>, ResendClientConfigureOptions>()
    .AddHttpClient<ResendClient>().Services
    .AddTransient<IResend, ResendClient>()
    .AddTransient<IEmailSender, EmailSender>()
    .AddRazorPages().Services
    .Configure<CorsPolicy>(corsPolicySection)
    .AddCors()
    .AddAzureClients(configureClients =>
    {
        configureClients.AddSecretClient(keyVaultSection);
    });
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
    builder.Services.Configure<IdentityPasskeyOptions>(options =>
    {
        // Allow https://localhost:7261 origin.
        options.ValidateOrigin = context => ValueTask.FromResult(context.Origin == "https://localhost:7261");
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler("/Error");
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseIdentityServer();
app.UseCors(corsPolicyBuilder =>
{
    var corsPolicy = app.Services.GetRequiredService<IOptions<CorsPolicy>>().Value;
    corsPolicyBuilder.WithOrigins(corsPolicy.Origins.ToArray());
});
app.UseAuthorization();
app.MapAdditionalIdentityEndpoints();
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets()
   .RequireAuthorization();
app.Run();
