using Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var assembly = typeof(ApplicationDbContext).Assembly;

builder.Services
    .AddDbContext<ApplicationDbContext>(dbContextOptionsBuilder => { dbContextOptionsBuilder.UseSqlServer(connectionString, sqlServerDbContextOptionsBuilder => { sqlServerDbContextOptionsBuilder.MigrationsAssembly(assembly); }); })
    .AddDatabaseDeveloperPageExceptionFilter()
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
    })
    .AddConfigurationStore<ApplicationDbContext>(configurationStoreOptions => { })
    .AddOperationalStore<ApplicationDbContext>(operationalStoreOptions => { })
    .AddAspNetIdentity<IdentityUser<Guid>>()
    .AddLicenseSummary().Services
    .AddAuthentication().Services
    .AddRazorPages();

var app = builder.Build();
var db = app.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>().Database;
db.Migrate();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets()
   .RequireAuthorization();
app.Run();
return;
