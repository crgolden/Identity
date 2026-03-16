namespace Identity.Tests.Infrastructure;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> for E2E tests.
/// Replaces only <see cref="IEmailSender"/> with <see cref="EmailCaptureService"/>.
/// All other services (SQL Server, Azure Key Vault, Blob Storage, etc.) are real.
/// </summary>
public sealed class IdentityWebApplicationFactory : WebApplicationFactory<Program>
{
    public EmailCaptureService EmailCapture { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);

        builder.ConfigureServices(services =>
        {
            // Remove the real IEmailSender registrations and replace with the capture service.
            services.RemoveAll<IEmailSender>();
            services.RemoveAll<IEmailSender<IdentityUser<Guid>>>();

            services.AddSingleton<EmailCaptureService>(_ => EmailCapture);
            services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<EmailCaptureService>());
            services.AddSingleton<IEmailSender<IdentityUser<Guid>>>(sp => sp.GetRequiredService<EmailCaptureService>());
        });
    }
}
