namespace Identity.Tests.Extensions;

using System.Collections.Generic;
using Identity.Extensions;
using Infrastructure;
using Microsoft.Extensions.Configuration;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class ConfigurationExtensionsTests
{
    [Fact]
    public void GetRequired_ReturnsValue_WhenKeyExists()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Foo"] = "bar" })
            .Build();

        Assert.Equal("bar", config.GetRequired<string>("Foo"));
    }

    [Fact]
    public void GetRequired_ThrowsInvalidOperationExceptionWithKeyName_WhenKeyMissing()
    {
        IConfiguration config = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<InvalidOperationException>(() => config.GetRequired<string>("Missing"));
        Assert.Equal("Invalid 'Missing'.", ex.Message);
    }

    [Fact]
    public void GetIdentitySecrets_ReadsAllConfiguredKeys()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GoogleClientId"] = "google-id",
                ["GoogleClientSecret"] = "google-secret",
                ["ServiceBusConnectionString"] = "sb-conn",
                ["GravatarApiSecretKey"] = "gravatar-key",
                ["ReCAPTCHASiteKey"] = "recaptcha-site",
                ["ReCAPTCHASecretKey"] = "recaptcha-secret",
                ["AdminEmail"] = "admin@example.com",
                ["TestEmail"] = "test@example.com",
            })
            .Build();

        var secrets = config.GetIdentitySecrets();

        Assert.Equal("google-id", secrets.GoogleClientId);
        Assert.Equal("google-secret", secrets.GoogleClientSecret);
        Assert.Equal("sb-conn", secrets.ServiceBusConnectionString);
        Assert.Equal("gravatar-key", secrets.GravatarApiSecretKey);
        Assert.Equal("recaptcha-site", secrets.ReCAPTCHASiteKey);
        Assert.Equal("recaptcha-secret", secrets.ReCAPTCHASecretKey);
        Assert.Equal("admin@example.com", secrets.AdminEmail);
        Assert.Equal("test@example.com", secrets.TestEmail);
    }

    [Fact]
    public void GetIdentitySecrets_LeavesOptionalEmailsNull_WhenAbsent()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GoogleClientId"] = "google-id",
                ["GoogleClientSecret"] = "google-secret",
                ["ServiceBusConnectionString"] = "sb-conn",
                ["GravatarApiSecretKey"] = "gravatar-key",
                ["ReCAPTCHASiteKey"] = "recaptcha-site",
                ["ReCAPTCHASecretKey"] = "recaptcha-secret",
            })
            .Build();

        var secrets = config.GetIdentitySecrets();

        Assert.Null(secrets.AdminEmail);
        Assert.Null(secrets.TestEmail);
    }
}
