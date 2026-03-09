using Azure.Security.KeyVault.Secrets;
using Elastic.Clients.Elasticsearch;
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
using System.Net.Http.Headers;

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
    .AddHttpClient<IGravatar, Gravatar>((sp, httpClient) =>
    {
        var secretClient = sp.GetRequiredService<SecretClient>();
        var gravatarApiKeySecret = secretClient.GetSecret("GravatarApiSecretKey").Value;
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.BearerScheme, gravatarApiKeySecret.Value);
    }).Services
    .AddScoped<IAvatarService, GravatarService>()
    .AddRazorPages().Services
    .Configure<CorsPolicy>(corsPolicySection)
    .AddCors()
    .AddHealthChecks().AddDbContextCheck<ApplicationDbContext>().Services
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

var uri = new Uri("http://192.168.0.33:9200/");
var settings = new ElasticsearchClientSettings(uri);
var client = new ElasticsearchClient(settings);
await client.Indices.CreateAsync("identity");
var doc1 = new
{
    Id = 1,
    User = "flobernd",
    Message = "Trying out the client, so far so good?"
};

var response1 = await client.IndexAsync(doc1, "identity");
var response2 = await client.GetAsync<object>(1, idx => idx.Index("identity"));
if (response2.IsValidResponse)
{
    var doc2 = response2.Source;
}

var response3 = await client.DeleteAsync("identity", 1);

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
app.MapHealthChecks("health");
app.MapRazorPages()
   .WithStaticAssets()
   .RequireAuthorization();
app.Run();
