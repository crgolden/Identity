namespace Identity.Tests.Extensions;
using Identity.Tests.Infrastructure;

using Azure.Core;
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
[Collection(UnitCollection.Name)]
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
    /// Verifies that AddObservability throws InvalidOperationException when ElasticsearchNode is absent.
    /// Input: no ElasticsearchNode configuration.
    /// Expected: InvalidOperationException with message "Invalid 'ElasticsearchNode'.".
    /// </summary>
    [Fact]
    public void AddObservability_MissingElasticsearchNode_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new Mock<IHostApplicationBuilder>();
        var configuration = new Mock<IConfigurationManager>();
        var elasticsearchSection = new Mock<IConfigurationSection>();
        elasticsearchSection.SetupGet(x => x.Path).Returns("ElasticsearchNode");
        elasticsearchSection.SetupGet(x => x.Value).Returns((string?)null);
        configuration.Setup(x => x.GetSection("ElasticsearchNode")).Returns(elasticsearchSection.Object);
        builder.SetupGet(x => x.Configuration).Returns(configuration.Object);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => builder.Object.AddObservability(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
        Assert.Equal("Invalid 'ElasticsearchNode'.", ex.Message);
    }

    /// <summary>
    /// Verifies that AddPersistence throws InvalidOperationException when SqlConnectionStringBuilder section is absent.
    /// Input: no SqlConnectionStringBuilder configuration.
    /// Expected: InvalidOperationException with message "Invalid 'SqlConnectionStringBuilder' section.".
    /// </summary>
    [Fact]
    public void AddPersistence_MissingSqlConnectionStringBuilderSection_ThrowsInvalidOperationException()
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

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => builder.Object.AddPersistence(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), null!, null!, null!));
        Assert.Equal("Invalid 'SqlConnectionStringBuilder' section.", ex.Message);
    }

    /// <summary>
    /// Verifies that AddEmail registers IEmailSender in the service collection.
    /// Input: a canned resend API token string.
    /// Expected: IEmailSender descriptor is present in builder.Services and builder is returned.
    /// </summary>
    [Fact]
    public void AddEmail_RegistersEmailSenderService()
    {
        // Arrange
        var builder = new Mock<IHostApplicationBuilder>();
        var serviceCollection = new ServiceCollection();
        builder.SetupGet(x => x.Services).Returns(serviceCollection);

        // Act
        var result = builder.Object.AddEmail("test-token");

        // Assert
        Assert.Same(builder.Object, result);
        Assert.Contains(serviceCollection, sd => sd.ServiceType == typeof(IEmailSender));
    }

    /// <summary>
    /// Verifies that AddEmail configures ResendClientOptions.ApiToken with the value passed in.
    /// Input: "my-resend-token" string.
    /// Expected: the registered IConfigureOptions action sets ApiToken to "my-resend-token" when invoked.
    /// </summary>
    [Fact]
    public void AddEmail_SetsApiTokenFromParameter()
    {
        // Arrange
        var builder = new Mock<IHostApplicationBuilder>();
        var serviceCollection = new ServiceCollection();
        builder.SetupGet(x => x.Services).Returns(serviceCollection);

        // Act
        builder.Object.AddEmail("my-resend-token");

        // Assert
        var descriptor = serviceCollection.First(sd => sd.ServiceType == typeof(IConfigureOptions<ResendClientOptions>));
        var configure = (IConfigureOptions<ResendClientOptions>)descriptor.ImplementationInstance!;
        var options = new ResendClientOptions();
        configure.Configure(options);
        Assert.Equal("my-resend-token", options.ApiToken);
    }

    /// <summary>
    /// Verifies that AddPicture registers IAvatarService in the service collection.
    /// Input: a canned Gravatar API key string.
    /// Expected: IAvatarService descriptor is present in builder.Services and builder is returned.
    /// </summary>
    [Fact]
    public void AddPicture_RegistersAvatarService()
    {
        // Arrange
        var builder = new Mock<IHostApplicationBuilder>();
        var serviceCollection = new ServiceCollection();
        builder.SetupGet(x => x.Services).Returns(serviceCollection);

        // Act
        var result = builder.Object.AddPicture("gravatar-key");

        // Assert
        Assert.Same(builder.Object, result);
        Assert.Contains(serviceCollection, sd => sd.ServiceType == typeof(IAvatarService));
    }

    /// <summary>
    /// Verifies that AddAuth registers IAuthenticationService in the service collection.
    /// Input: canned Google OAuth credential strings.
    /// Expected: IAuthenticationService descriptor is present in builder.Services and builder is returned.
    /// </summary>
    [Fact]
    public void AddAuth_RegistersAuthenticationServices()
    {
        // Arrange
        var builder = new Mock<IHostApplicationBuilder>();
        var serviceCollection = new ServiceCollection();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns("Production");
        builder.SetupGet(x => x.Services).Returns(serviceCollection);
        builder.SetupGet(x => x.Environment).Returns(environment.Object);

        // Act
        var result = builder.Object.AddAuth("google-id", "google-secret");

        // Assert
        Assert.Same(builder.Object, result);
        Assert.Contains(serviceCollection, sd => sd.ServiceType == typeof(IAuthenticationService));
    }
}
