namespace Identity.Tests;

using System.Security.Cryptography;
using System.Text;
using Identity;
using Moq;

[Trait("Category", "Unit")]
public class GravatarServiceTests
{
    [Fact]
    public async Task GetAvatarUrlAsync_ProfileFound_ReturnsAvatarUrl()
    {
        // Arrange
        var expected = new Uri("https://gravatar.com/avatar/abc123");
        var profile = new Profile { Avatar_url = expected };
        var gravatarMock = new Mock<IGravatar>();
        gravatarMock
            .Setup(g => g.GetProfileByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        var service = new GravatarService(gravatarMock.Object);

        // Act
        var result = await service.GetAvatarUrlAsync("user@example.com", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetAvatarUrlAsync_ProfileReturnsNullAvatarUrl_ReturnsNull()
    {
        // Arrange
        var profile = new Profile { Avatar_url = null! };
        var gravatarMock = new Mock<IGravatar>();
        gravatarMock
            .Setup(g => g.GetProfileByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        var service = new GravatarService(gravatarMock.Object);

        // Act
        var result = await service.GetAvatarUrlAsync("user@example.com", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAvatarUrlAsync_ProfileNotFound_ReturnsNull()
    {
        // Arrange
        var gravatarMock = new Mock<IGravatar>();
        gravatarMock
            .Setup(g => g.GetProfileByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException("Not Found", 404, string.Empty, new Dictionary<string, IEnumerable<string>>(), null!));
        var service = new GravatarService(gravatarMock.Object);

        // Act
        var result = await service.GetAvatarUrlAsync("unknown@example.com", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAvatarUrlAsync_NonNotFoundApiException_PropagatesException()
    {
        // Arrange
        var gravatarMock = new Mock<IGravatar>();
        gravatarMock
            .Setup(g => g.GetProfileByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException("Internal Server Error", 500, string.Empty, new Dictionary<string, IEnumerable<string>>(), null!));
        var service = new GravatarService(gravatarMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(() => service.GetAvatarUrlAsync("user@example.com", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetAvatarUrlAsync_AlwaysHashesEmailToSha256Lowercase()
    {
        // Arrange
        const string profileIdentifier = "User@Example.COM";
        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(profileIdentifier))).ToLowerInvariant();

        var capturedHash = string.Empty;
        var gravatarMock = new Mock<IGravatar>();
        gravatarMock
            .Setup(g => g.GetProfileByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((h, _) => capturedHash = h)
            .ReturnsAsync((Profile?)null);
        var service = new GravatarService(gravatarMock.Object);

        // Act
        await service.GetAvatarUrlAsync(profileIdentifier, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedHash, capturedHash);
        Assert.Equal(capturedHash, capturedHash.ToLowerInvariant()); // must be lowercase
        Assert.Equal(64, capturedHash.Length); // SHA-256 = 32 bytes = 64 hex chars
    }

    [Fact]
    public async Task GetAvatarUrlAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        CancellationToken capturedToken = default;
        var profile = new Profile { Avatar_url = new Uri("https://gravatar.com/avatar/test") };
        var gravatarMock = new Mock<IGravatar>();
        gravatarMock
            .Setup(g => g.GetProfileByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, ct) => capturedToken = ct)
            .ReturnsAsync(profile);
        var service = new GravatarService(gravatarMock.Object);

        // Act
        await service.GetAvatarUrlAsync("user@example.com", cts.Token);

        // Assert
        Assert.Equal(cts.Token, capturedToken);
    }
}