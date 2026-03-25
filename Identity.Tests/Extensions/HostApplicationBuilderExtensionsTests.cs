namespace Identity.Tests.Extensions;

using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Identity.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Resend;

/// <summary>Unit tests for <see cref="HostApplicationBuilderExtensions"/> extension methods.</summary>
[Trait("Category", "Unit")]
public sealed class HostApplicationBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that AddCors throws InvalidOperationException when the CorsPolicy section is absent.
    /// Input: no CorsPolicy configuration.
    /// Expected: InvalidOperationException with message "Missing 'CorsPolicy' section.".
    /// </summary>
    [Fact]
    public void AddCors_MissingCorsPolicySection_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new Mock<IHostApplicationBuilder>();
        var configuration = new Mock<IConfigurationManager>();
        var corsPolicySection = new Mock<IConfigurationSection>();
        corsPolicySection.SetupGet(x => x.Value).Returns((string?)null);
        corsPolicySection.Setup(x => x.GetChildren()).Returns([]);
        configuration.Setup(x => x.GetSection("CorsPolicy")).Returns(corsPolicySection.Object);
        builder.SetupGet(x => x.Configuration).Returns(configuration.Object);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Object.AddCors());
        Assert.Equal("Missing 'CorsPolicy' section.", ex.Message);
    }

    /// <summary>
    /// Verifies that AddCors registers ICorsService when the CorsPolicy section is present.
    /// Input: configuration containing a CorsPolicy section.
    /// Expected: ICorsService is registered in the service collection and builder is returned.
    /// </summary>
    [Fact]
    public void AddCors_ValidCorsPolicySection_RegistersCorsService()
    {
        // Arrange
        var builder = new Mock<IHostApplicationBuilder>();
        var configuration = new Mock<IConfigurationManager>();
        var serviceCollection = new ServiceCollection();
        var corsPolicySection = new Mock<IConfigurationSection>();
        corsPolicySection.SetupGet(x => x.Key).Returns("CorsPolicy");
        corsPolicySection.SetupGet(x => x.Path).Returns("CorsPolicy");
        corsPolicySection.SetupGet(x => x.Value).Returns("true");
        configuration.Setup(x => x.GetSection("CorsPolicy")).Returns(corsPolicySection.Object);
        builder.SetupGet(x => x.Configuration).Returns(configuration.Object);
        builder.SetupGet(x => x.Services).Returns(serviceCollection);

        // Act
        var result = builder.Object.AddCors();

        // Assert
        Assert.Same(builder.Object, result);
        Assert.Contains(serviceCollection, sd => sd.ServiceType == typeof(ICorsService));
    }

    /// <summary>
    /// Verifies that AddDataProtection throws InvalidOperationException when BlobUri is absent.
    /// Input: configuration with no BlobUri; mock TokenCredential.
    /// Expected: InvalidOperationException with message containing "BlobUri".
    /// </summary>
    [Fact]
    public void AddDataProtection_MissingBlobUri_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new Mock<IHostApplicationBuilder>();
        var configuration = new Mock<IConfigurationManager>();
        var blobSection = new Mock<IConfigurationSection>();
        blobSection.SetupGet(x => x.Path).Returns("BlobUri");
        blobSection.SetupGet(x => x.Value).Returns((string?)null);
        configuration.Setup(x => x.GetSection("BlobUri")).Returns(blobSection.Object);
        builder.SetupGet(x => x.Configuration).Returns(configuration.Object);
        var credential = new Mock<TokenCredential>();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Object.AddDataProtection(credential.Object));
        Assert.Contains("BlobUri", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that AddDataProtection throws InvalidOperationException when DataProtectionKeyIdentifier is absent.
    /// Input: configuration with BlobUri but no DataProtectionKeyIdentifier; mock TokenCredential.
    /// Expected: InvalidOperationException with message containing "DataProtectionKeyIdentifier".
    /// </summary>
    [Fact]
    public void AddDataProtection_MissingDataProtectionKeyIdentifier_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new Mock<IHostApplicationBuilder>();
        var configuration = new Mock<IConfigurationManager>();
        var blobSection = new Mock<IConfigurationSection>();
        blobSection.SetupGet(x => x.Path).Returns("BlobUri");
        blobSection.SetupGet(x => x.Value).Returns("https://mystorage.blob.core.windows.net/keys/dp-keys.xml");
        var dpSection = new Mock<IConfigurationSection>();
        dpSection.SetupGet(x => x.Path).Returns("DataProtectionKeyIdentifier");
        dpSection.SetupGet(x => x.Value).Returns((string?)null);
        configuration.Setup(x => x.GetSection("BlobUri")).Returns(blobSection.Object);
        configuration.Setup(x => x.GetSection("DataProtectionKeyIdentifier")).Returns(dpSection.Object);
        builder.SetupGet(x => x.Configuration).Returns(configuration.Object);
        var credential = new Mock<TokenCredential>();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Object.AddDataProtection(credential.Object));
        Assert.Contains("DataProtectionKeyIdentifier", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that AddObservabilityAsync throws InvalidOperationException when ElasticsearchNode is absent.
    /// Input: no ElasticsearchNode configuration; mock SecretClient.
    /// Expected: InvalidOperationException with message "Invalid 'ElasticsearchNode'.".
    /// </summary>
    [Fact]
    public async Task AddObservabilityAsync_MissingElasticsearchNode_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new Mock<IHostApplicationBuilder>();
        var configuration = new Mock<IConfigurationManager>();
        var elasticsearchSection = new Mock<IConfigurationSection>();
        elasticsearchSection.SetupGet(x => x.Path).Returns("ElasticsearchNode");
        elasticsearchSection.SetupGet(x => x.Value).Returns((string?)null);
        configuration.Setup(x => x.GetSection("ElasticsearchNode")).Returns(elasticsearchSection.Object);
        builder.SetupGet(x => x.Configuration).Returns(configuration.Object);
        var secretClient = new Mock<SecretClient>();
        var ct = TestContext.Current.CancellationToken;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => builder.Object.AddObservabilityAsync(secretClient.Object, ct));
        Assert.Equal("Invalid 'ElasticsearchNode'.", ex.Message);
    }

    /// <summary>
    /// Verifies that AddPersistenceAsync throws InvalidOperationException when SqlConnectionStringBuilder section is absent.
    /// Input: no SqlConnectionStringBuilder configuration; mock SecretClient; builder args unused before the throw.
    /// Expected: InvalidOperationException with message "Invalid 'SqlConnectionStringBuilder' section.".
    /// </summary>
    [Fact]
    public async Task AddPersistenceAsync_MissingSqlConnectionStringBuilderSection_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new Mock<IHostApplicationBuilder>();
        var configuration = new Mock<IConfigurationManager>();
        var sqlSection = new Mock<IConfigurationSection>();
        sqlSection.SetupGet(x => x.Path).Returns("SqlConnectionStringBuilder");
        sqlSection.SetupGet(x => x.Value).Returns((string?)null);
        sqlSection.Setup(x => x.GetChildren()).Returns([]);
        configuration.Setup(x => x.GetSection("SqlConnectionStringBuilder")).Returns(sqlSection.Object);
        builder.SetupGet(x => x.Configuration).Returns(configuration.Object);
        var secretClient = new Mock<SecretClient>();
        var ct = TestContext.Current.CancellationToken;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => builder.Object.AddPersistenceAsync(secretClient.Object, null!, null!, null!, ct));
        Assert.Equal("Invalid 'SqlConnectionStringBuilder' section.", ex.Message);
    }

    /// <summary>
    /// Verifies that AddEmailAsync registers IEmailSender in the service collection.
    /// Input: mock SecretClient returning a canned ResendApiToken value.
    /// Expected: IEmailSender descriptor is present in builder.Services; builder is returned; secret fetched once.
    /// </summary>
    [Fact]
    public async Task AddEmailAsync_RegistersEmailSenderService()
    {
        // Arrange
        var builder = new Mock<IHostApplicationBuilder>();
        var serviceCollection = new ServiceCollection();
        builder.SetupGet(x => x.Services).Returns(serviceCollection);
        var secret = new KeyVaultSecret("ResendApiToken", "test-token");
        var response = Response.FromValue(secret, Mock.Of<Response>());
        var secretClient = new Mock<SecretClient>();
        secretClient
            .Setup(x => x.GetSecretAsync("ResendApiToken", null, null, TestContext.Current.CancellationToken))
            .ReturnsAsync(response);

        // Act
        var result = await builder.Object.AddEmailAsync(secretClient.Object, TestContext.Current.CancellationToken);

        // Assert
        Assert.Same(builder.Object, result);
        Assert.Contains(serviceCollection, sd => sd.ServiceType == typeof(IEmailSender));
        secretClient.Verify(x => x.GetSecretAsync("ResendApiToken", null, null, TestContext.Current.CancellationToken), Times.Once);
    }

    /// <summary>
    /// Verifies that AddEmailAsync configures ResendClientOptions.ApiToken with the value retrieved from Key Vault.
    /// Input: mock SecretClient returning "my-resend-token" for ResendApiToken.
    /// Expected: the registered IConfigureOptions action sets ApiToken to "my-resend-token" when invoked.
    /// </summary>
    [Fact]
    public async Task AddEmailAsync_SetsApiTokenFromSecret()
    {
        // Arrange
        var builder = new Mock<IHostApplicationBuilder>();
        var serviceCollection = new ServiceCollection();
        builder.SetupGet(x => x.Services).Returns(serviceCollection);
        var secret = new KeyVaultSecret("ResendApiToken", "my-resend-token");
        var response = Response.FromValue(secret, Mock.Of<Response>());
        var secretClient = new Mock<SecretClient>();
        secretClient
            .Setup(x => x.GetSecretAsync("ResendApiToken", null, null, TestContext.Current.CancellationToken))
            .ReturnsAsync(response);

        // Act
        await builder.Object.AddEmailAsync(secretClient.Object, TestContext.Current.CancellationToken);

        // Assert
        var descriptor = serviceCollection.First(sd => sd.ServiceType == typeof(IConfigureOptions<ResendClientOptions>));
        var configure = (IConfigureOptions<ResendClientOptions>)descriptor.ImplementationInstance!;
        var options = new ResendClientOptions();
        configure.Configure(options);
        Assert.Equal("my-resend-token", options.ApiToken);
    }

    /// <summary>
    /// Verifies that AddPictureAsync registers IAvatarService in the service collection.
    /// Input: mock SecretClient returning a canned GravatarApiSecretKey value.
    /// Expected: IAvatarService descriptor is present in builder.Services; builder is returned; secret fetched once.
    /// </summary>
    [Fact]
    public async Task AddPictureAsync_RegistersAvatarService()
    {
        // Arrange
        var builder = new Mock<IHostApplicationBuilder>();
        var serviceCollection = new ServiceCollection();
        builder.SetupGet(x => x.Services).Returns(serviceCollection);
        var secret = new KeyVaultSecret("GravatarApiSecretKey", "gravatar-key");
        var response = Response.FromValue(secret, Mock.Of<Response>());
        var secretClient = new Mock<SecretClient>();
        secretClient
            .Setup(x => x.GetSecretAsync("GravatarApiSecretKey", null, null, TestContext.Current.CancellationToken))
            .ReturnsAsync(response);

        // Act
        var result = await builder.Object.AddPictureAsync(secretClient.Object, TestContext.Current.CancellationToken);

        // Assert
        Assert.Same(builder.Object, result);
        Assert.Contains(serviceCollection, sd => sd.ServiceType == typeof(IAvatarService));
        secretClient.Verify(x => x.GetSecretAsync("GravatarApiSecretKey", null, null, TestContext.Current.CancellationToken), Times.Once);
    }

    /// <summary>
    /// Verifies that AddAuthAsync registers IAuthenticationService in the service collection.
    /// Input: mock SecretClient returning canned Google OAuth credentials.
    /// Expected: IAuthenticationService descriptor is present in builder.Services; builder is returned; both secrets fetched once.
    /// </summary>
    [Fact]
    public async Task AddAuthAsync_RegistersAuthenticationServices()
    {
        // Arrange
        var builder = new Mock<IHostApplicationBuilder>();
        var serviceCollection = new ServiceCollection();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns("Production");
        builder.SetupGet(x => x.Services).Returns(serviceCollection);
        builder.SetupGet(x => x.Environment).Returns(environment.Object);
        var secret1 = new KeyVaultSecret("GoogleClientId", "google-id");
        var secret2 = new KeyVaultSecret("GoogleClientSecret", "google-secret");
        var response1 = Response.FromValue(secret1, Mock.Of<Response>());
        var response2 = Response.FromValue(secret2, Mock.Of<Response>());
        var secretClient = new Mock<SecretClient>();
        secretClient
            .Setup(x => x.GetSecretAsync("GoogleClientId", null, null, TestContext.Current.CancellationToken))
            .ReturnsAsync(response1);
        secretClient
            .Setup(x => x.GetSecretAsync("GoogleClientSecret", null, null, TestContext.Current.CancellationToken))
            .ReturnsAsync(response2);

        // Act
        var result = await builder.Object.AddAuthAsync(secretClient.Object, TestContext.Current.CancellationToken);

        // Assert
        Assert.Same(builder.Object, result);
        Assert.Contains(serviceCollection, sd => sd.ServiceType == typeof(IAuthenticationService));
        secretClient.Verify(x => x.GetSecretAsync("GoogleClientId", null, null, TestContext.Current.CancellationToken), Times.Once);
        secretClient.Verify(x => x.GetSecretAsync("GoogleClientSecret", null, null, TestContext.Current.CancellationToken), Times.Once);
    }
}
