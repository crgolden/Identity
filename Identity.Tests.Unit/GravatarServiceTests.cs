namespace Identity.Tests.Unit;

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Identity.Avatar;
using Identity.Tests.Unit.Infrastructure;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class GravatarServiceTests : IDisposable
{
    private readonly TelemetryHarness _harness = new();

    [Fact]
    public void IsOwnComputedUrl_UrlOnTheGravatarHost_IsTrue()
    {
        // Arrange
        var gravatarUrl = AbsoluteUrlOn(GravatarService.GravatarHost);
        var service = new GravatarService(_harness.Telemetry);

        // Act
        var isOwn = service.IsOwnComputedUrl(gravatarUrl);

        // Assert
        Assert.True(isOwn);
    }

    [Fact]
    public void IsOwnComputedUrl_UrlOnASingleLetterSubdomainOfTheGravatarHost_IsTrue()
    {
        // Arrange
        var singleLetterSubdomain = Generated.LowercaseToken(1);
        var subdomainUrl = AbsoluteUrlOn(singleLetterSubdomain + '.' + GravatarService.GravatarHost);
        var service = new GravatarService(_harness.Telemetry);

        // Act
        var isOwn = service.IsOwnComputedUrl(subdomainUrl);

        // Assert
        Assert.True(isOwn);
    }

    [Fact]
    public void IsOwnComputedUrl_UrlOnAHostLabelSubdomainOfTheGravatarHost_IsTrue()
    {
        // Arrange
        var subdomainLabel = Generated.NewHostLabel();
        var subdomainUrl = AbsoluteUrlOn(subdomainLabel + '.' + GravatarService.GravatarHost);
        var service = new GravatarService(_harness.Telemetry);

        // Act
        var isOwn = service.IsOwnComputedUrl(subdomainUrl);

        // Assert
        Assert.True(isOwn);
    }

    [Fact]
    public void IsOwnComputedUrl_UrlOnTheUpperCaseGravatarHost_IsTrue()
    {
        // Arrange
        var upperCaseHostUrl = AbsoluteUrlOn(GravatarService.GravatarHost.ToUpperInvariant());
        var service = new GravatarService(_harness.Telemetry);

        // Act
        var isOwn = service.IsOwnComputedUrl(upperCaseHostUrl);

        // Assert
        Assert.True(isOwn);
    }

    [Fact]
    public void IsOwnComputedUrl_UrlOnAnExternalHost_IsFalse()
    {
        // Arrange
        var externalHost = Generated.NewExternalHost();
        var externalUrl = AbsoluteUrlOn(externalHost);
        var service = new GravatarService(_harness.Telemetry);

        // Act
        var isOwn = service.IsOwnComputedUrl(externalUrl);

        // Assert
        Assert.False(isOwn);
    }

    [Fact]
    public void IsOwnComputedUrl_UrlOnAHostEndingInTheGravatarHostWithoutADot_IsFalse()
    {
        // Arrange
        var hostLabelPrefix = Generated.NewHostLabel();
        var lookalikeUrl = AbsoluteUrlOn(hostLabelPrefix + GravatarService.GravatarHost);
        var service = new GravatarService(_harness.Telemetry);

        // Act
        var isOwn = service.IsOwnComputedUrl(lookalikeUrl);

        // Assert
        Assert.False(isOwn);
    }

    [Fact]
    public void IsOwnComputedUrl_NotAUrl_IsFalse()
    {
        // Arrange
        var notAUrl = Generated.NewValidationMessage();
        var service = new GravatarService(_harness.Telemetry);

        // Act
        var isOwn = service.IsOwnComputedUrl(notAUrl);

        // Assert
        Assert.False(isOwn);
    }

    [Fact]
    public async Task GetAvatarUrlAsync_NormalizesTheEmailBeforeHashing()
    {
        // Arrange
        var canonicalAddress = Generated.NewEmailAddress();
        var expectedHash = ExpectedHash(canonicalAddress);
        var service = new GravatarService(_harness.Telemetry);

        // Act
        var fromCanonical = await service.GetAvatarUrlAsync(canonicalAddress, TestContext.Current.CancellationToken);
        var fromUpperCase = await service.GetAvatarUrlAsync(
            canonicalAddress.ToUpperInvariant(),
            TestContext.Current.CancellationToken);
        var fromPadded = await service.GetAvatarUrlAsync(
            Generated.NewWhitespaceValue() + canonicalAddress + Generated.NewWhitespaceValue(),
            TestContext.Current.CancellationToken);
        var fromMixedCase = await service.GetAvatarUrlAsync(
            WithUpperCaseDomain(canonicalAddress),
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
        var emailAddress = Generated.NewEmailAddress();
        var service = new GravatarService(_harness.Telemetry);

        // Act
        var result = await service.GetAvatarUrlAsync(emailAddress, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            GravatarService.ImageBaseUrl + ExpectedHash(emailAddress) + GravatarService.DefaultImageQuery,
            result?.ToString());
    }

    [Fact]
    public async Task GetAvatarUrlAsync_ResolvesAnImageForAnAddressWithNoGravatarAccount()
    {
        // Arrange
        var registeredAddress = Generated.NewEmailAddress();
        var unregisteredAddress = $"{Guid.NewGuid()}@example.invalid";
        var service = new GravatarService(_harness.Telemetry);

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
    public async Task GetAvatarUrlAsync_BuildsTheUrlWithoutAnOutboundCall()
    {
        // Arrange
        var emailAddress = Generated.NewEmailAddress();
        var service = new GravatarService(_harness.Telemetry);

        // Act
        var result = await service.GetAvatarUrlAsync(emailAddress, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAvatarUrlAsync_HonoursCancellation()
    {
        // Arrange
        var emailAddress = Generated.NewEmailAddress();
        var service = new GravatarService(_harness.Telemetry);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var exception = await Record.ExceptionAsync(() => service.GetAvatarUrlAsync(emailAddress, cts.Token));

        // Assert
        Assert.IsType<OperationCanceledException>(exception, exactMatch: false);
    }

    [Fact]
    public async Task GetAvatarUrlAsync_TagsTheActivityWithTheNormalizedHash()
    {
        // Arrange
        var canonicalAddress = Generated.NewEmailAddress();
        var expectedHash = ExpectedHash(canonicalAddress);
        string? capturedOperationName = null;
        string? capturedHashTag = null;
        using var listener = new ActivityListener();
        listener.ShouldListenTo = source => string.Equals(source.Name, Telemetry.SourceName, StringComparison.Ordinal);
        listener.Sample = (ref _) => ActivitySamplingResult.AllData;
        listener.ActivityStopped = activity =>
        {
            capturedOperationName = activity.OperationName;
            capturedHashTag = activity.GetTagItem(GravatarService.HashTagName)?.ToString();
        };
        ActivitySource.AddActivityListener(listener);
        var service = new GravatarService(_harness.Telemetry);

        // Act
        await service.GetAvatarUrlAsync(WithUpperCaseDomain(canonicalAddress), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(GravatarService.ActivityName, capturedOperationName);
        Assert.Equal(expectedHash, capturedHashTag);
    }

    public void Dispose() => _harness.Dispose();

    private static string AbsoluteUrlOn(string host) =>
        Uri.UriSchemeHttps + Uri.SchemeDelimiter + host + '/' + Generated.NewPathSegment();

    private static string WithUpperCaseDomain(string emailAddress)
    {
        var atIndex = emailAddress.IndexOf('@', StringComparison.Ordinal);
        return emailAddress[..atIndex] + emailAddress[atIndex..].ToUpperInvariant();
    }

    private static string ExpectedHash(string address) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(address.ToLowerInvariant()))).ToLowerInvariant();
}
