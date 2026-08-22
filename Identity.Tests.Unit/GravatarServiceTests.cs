namespace Identity.Tests.Unit;

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Identity;
using Identity.Avatar;
using Infrastructure;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class GravatarServiceTests
{
    [Theory]
    [InlineData("https://" + GravatarService.GravatarHost + "/avatar/abc", true)]
    [InlineData("https://0." + GravatarService.GravatarHost + "/avatar/abc", true)]
    [InlineData("https://secure." + GravatarService.GravatarHost + "/avatar/abc", true)]
    [InlineData("https://GRAVATAR.COM/avatar/abc", true)]
    [InlineData("https://lh3.googleusercontent.com/abc", false)]
    [InlineData("https://example.test/avatar.jpg", false)]
    [InlineData("https://not" + GravatarService.GravatarHost + "/avatar/abc", false)]
    [InlineData("not a url", false)]
    public void IsOwnComputedUrl_RecognizesEveryGravatarHostAndNothingElse(string candidate, bool expected)
    {
        // Arrange
        var service = new GravatarService();

        // Act
        var actual = service.IsOwnComputedUrl(candidate);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetAvatarUrlAsync_NormalizesTheEmailBeforeHashing()
    {
        // Arrange
        var localPart = Guid.NewGuid().ToString();
        var canonicalAddress = $"{localPart}@example.com";
        var expectedHash = ExpectedHash(canonicalAddress);
        var service = new GravatarService();

        // Act
        var fromCanonical = await service.GetAvatarUrlAsync(canonicalAddress, TestContext.Current.CancellationToken);
        var fromUpperCase = await service.GetAvatarUrlAsync(
            canonicalAddress.ToUpperInvariant(),
            TestContext.Current.CancellationToken);
        var fromPadded = await service.GetAvatarUrlAsync(
            $"  {canonicalAddress}  ",
            TestContext.Current.CancellationToken);
        var fromMixedCase = await service.GetAvatarUrlAsync(
            $"{localPart}@Example.COM",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(expectedHash, fromCanonical?.ToString(), StringComparison.Ordinal);
        Assert.Contains(expectedHash, fromUpperCase?.ToString(), StringComparison.Ordinal);
        Assert.Contains(expectedHash, fromPadded?.ToString(), StringComparison.Ordinal);
        Assert.Contains(expectedHash, fromMixedCase?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAvatarUrlAsync_BuildsTheDocumentedImageUrl()
    {
        // Arrange
        var emailAddress = $"{Guid.NewGuid()}@example.com";
        var service = new GravatarService();

        // Act
        var result = await service.GetAvatarUrlAsync(emailAddress, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            $"{GravatarService.ImageBaseUrl}{ExpectedHash(emailAddress)}{GravatarService.DefaultImageQuery}",
            result?.ToString());
    }

    [Fact]
    public async Task GetAvatarUrlAsync_ResolvesAnImageForAnAddressWithNoGravatarAccount()
    {
        // Arrange
        var registeredAddress = $"{Guid.NewGuid()}@example.com";
        var unregisteredAddress = $"{Guid.NewGuid()}@example.invalid";
        var service = new GravatarService();

        // Act
        var registered = await service.GetAvatarUrlAsync(registeredAddress, TestContext.Current.CancellationToken);
        var unregistered = await service.GetAvatarUrlAsync(unregisteredAddress, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(registered);
        Assert.NotNull(unregistered);
        Assert.NotEqual(registered, unregistered);
        Assert.EndsWith(GravatarService.DefaultImageQuery, unregistered.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAvatarUrlAsync_ConstructsWithNoCollaboratorAndMakesNoOutboundCall()
    {
        // Arrange
        var emailAddress = $"{Guid.NewGuid()}@example.com";
        var service = new GravatarService();

        // Act
        var result = await service.GetAvatarUrlAsync(emailAddress, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAvatarUrlAsync_HonoursCancellation()
    {
        // Arrange
        var emailAddress = $"{Guid.NewGuid()}@example.com";
        var service = new GravatarService();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var exception = await Record.ExceptionAsync(() => service.GetAvatarUrlAsync(emailAddress, cts.Token));

        // Assert
        Assert.IsAssignableFrom<OperationCanceledException>(exception);
    }

    [Fact]
    public async Task GetAvatarUrlAsync_TagsTheActivityWithTheNormalizedHash()
    {
        // Arrange
        var localPart = Guid.NewGuid().ToString();
        var expectedHash = ExpectedHash($"{localPart}@example.com");
        string? capturedOperationName = null;
        string? capturedHashTag = null;
        const string activitySourceName = nameof(Identity);
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => string.Equals(source.Name, activitySourceName, StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                capturedOperationName = activity.OperationName;
                capturedHashTag = activity.GetTagItem(GravatarService.HashTagName)?.ToString();
            },
        };
        ActivitySource.AddActivityListener(listener);
        var service = new GravatarService();

        // Act
        await service.GetAvatarUrlAsync($"{localPart}@Example.COM", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(GravatarService.ActivityName, capturedOperationName);
        Assert.Equal(expectedHash, capturedHashTag);
    }

    private static string ExpectedHash(string address) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(address.ToLowerInvariant()))).ToLowerInvariant();
}
