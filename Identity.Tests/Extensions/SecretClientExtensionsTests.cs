namespace Identity.Tests.Extensions;

using System.Collections.Generic;
using System.Threading;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Identity.Extensions;
using Infrastructure;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class SecretClientExtensionsTests
{
    [Fact]
    public void GetIdentitySecrets_ReturnsTupleWithAllSecretValues()
    {
        var values = new Dictionary<string, string>
        {
            ["GoogleClientId"] = "google-id",
            ["GoogleClientSecret"] = "google-secret",
            ["GravatarApiSecretKey"] = "gravatar-key",
            ["IdentitySqlServerUserId"] = "sql-user",
            ["IdentitySqlServerPassword"] = "sql-pass",
            ["ElasticsearchUsername"] = "es-user",
            ["ElasticsearchPassword"] = "es-pass",
            ["ReCAPTCHASiteKey"] = "recaptcha-site",
            ["ReCAPTCHASecretKey"] = "recaptcha-secret",
            ["AdminEmail"] = "admin@example.com",
            ["TestEmail"] = "test@example.com",
        };
        var mock = new Mock<SecretClient>();
        mock.Setup(c => c.GetSecret(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()))
            .Returns<string, string?, SecretContentType?, CancellationToken>((name, _, _, _) => SecretResponse(name, values[name]));

        var secrets = mock.Object.GetIdentitySecrets();

        Assert.Equal("google-id", secrets.GoogleClientId.Value);
        Assert.Equal("google-secret", secrets.GoogleClientSecret.Value);
        Assert.Equal("gravatar-key", secrets.GravatarApiSecretKey.Value);
        Assert.Equal("sql-user", secrets.SqlServerUserId.Value);
        Assert.Equal("sql-pass", secrets.SqlServerPassword.Value);
        Assert.Equal("es-user", secrets.ElasticsearchUsername.Value);
        Assert.Equal("es-pass", secrets.ElasticsearchPassword.Value);
        Assert.Equal("recaptcha-site", secrets.ReCAPTCHASiteKey.Value);
        Assert.Equal("recaptcha-secret", secrets.ReCAPTCHASecretKey.Value);
        Assert.Equal("admin@example.com", secrets.AdminEmail.Value);
        Assert.Equal("test@example.com", secrets.TestEmail.Value);
    }

    private static Response<KeyVaultSecret> SecretResponse(string name, string value) =>
        Response.FromValue(new KeyVaultSecret(name, value), Mock.Of<Response>());
}
