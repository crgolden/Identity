using Google.Apis.Auth.AspNetCore3;
using Identity;
using Identity.Data;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Resend;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var corsPolicySection = builder.Configuration.GetSection(nameof(CorsPolicy));
var resendClientOptionsSection = builder.Configuration.GetSection(nameof(ResendClientOptions));
var assembly = typeof(ApplicationDbContext).Assembly;

builder.Services
    .AddDbContext<ApplicationDbContext>(dbContextOptionsBuilder => { dbContextOptionsBuilder.UseSqlServer(connectionString, sqlServerDbContextOptionsBuilder => { sqlServerDbContextOptionsBuilder.MigrationsAssembly(assembly); }); })
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
        identityServerOptions.UserInteraction.ErrorUrl = "/Identity/Error";
    })
    .AddConfigurationStore<ApplicationDbContext>(configurationStoreOptions => { })
    .AddOperationalStore<ApplicationDbContext>(operationalStoreOptions => { })
    .AddAspNetIdentity<IdentityUser<Guid>>()
    .AddLicenseSummary().Services
    .AddAuthentication()
    .AddGoogleOpenIdConnect(
        authenticationScheme: GoogleOpenIdConnectDefaults.AuthenticationScheme,
        displayName: "Google",
        configureOptions: openIdConnectOptions =>
        {
            openIdConnectOptions.SignInScheme = IdentityConstants.ExternalScheme;
            openIdConnectOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
            openIdConnectOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        }).Services
    .AddRazorPages().Services
    .AddHttpClient<ResendClient>().Services
    .Configure<CorsPolicy>(corsPolicySection)
    .Configure<ResendClientOptions>(resendClientOptionsSection)
    .AddTransient<IResend, ResendClient>()
    .AddTransient<IEmailSender, EmailSender>()
    .AddCors();
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
app.UseCors(corsPolicyBuilder =>
{
    var corsPolicy = app.Services.GetRequiredService<IOptions<CorsPolicy>>().Value;
    corsPolicyBuilder.WithOrigins(corsPolicy.Origins.ToArray());
});
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();
app.MapAdditionalIdentityEndpoints();
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets()
   .RequireAuthorization();
app.Run();
