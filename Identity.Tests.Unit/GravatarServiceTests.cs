namespace Identity.Tests.Unit;

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Identity.Avatar;
using Infrastructure;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class GravatarServiceTests
{
    public static TheoryData<string, bool> CandidateAvatarUrls() => new()
    {
        { AbsoluteUrlOn(GravatarService.GravatarHost), true },
        { AbsoluteUrlOn(TestValues.LowercaseToken(1) + '.' + GravatarService.GravatarHost), true },
        { AbsoluteUrlOn(TestValues.NewHostLabel() + '.' + GravatarService.GravatarHost), true },
        { AbsoluteUrlOn(GravatarService.GravatarHost.ToUpperInvariant()), true },
        { AbsoluteUrlOn(TestValues.NewExternalHost()), false },
        { AbsoluteUrlOn(TestValues.NewHostLabel() + GravatarService.GravatarHost), false },
        { TestValues.NewValidationMessage(), false },
    };

    [Theory]
    [MemberData(nameof(CandidateAvatarUrls))]
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
        var canonicalAddress = TestValues.NewEmailAddress();
        var expectedHash = ExpectedHash(canonicalAddress);
        var service = new GravatarService();

        // Act
        var fromCanonical = await service.GetAvatarUrlAsync(canonicalAddress, TestContext.Current.CancellationToken);
        var fromUpperCase = await service.GetAvatarUrlAsync(
            canonicalAddress.ToUpperInvariant(),
            TestContext.Current.CancellationToken);
        var fromPadded = await service.GetAvatarUrlAsync(
            TestValues.NewWhitespaceValue() + canonicalAddress + TestValues.NewWhitespaceValue(),
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
        var emailAddress = TestValues.NewEmailAddress();
        var service = new GravatarService();

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
        var registeredAddress = TestValues.NewEmailAddress();
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
        var emailAddress = TestValues.NewEmailAddress();
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
        var emailAddress = TestValues.NewEmailAddress();
        var service = new GravatarService();
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
        var canonicalAddress = TestValues.NewEmailAddress();
        var expectedHash = ExpectedHash(canonicalAddress);
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
        await service.GetAvatarUrlAsync(WithUpperCaseDomain(canonicalAddress), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(GravatarService.ActivityName, capturedOperationName);
        Assert.Equal(expectedHash, capturedHashTag);
    }

    private static string AbsoluteUrlOn(string host) =>
        Uri.UriSchemeHttps + Uri.SchemeDelimiter + host + '/' + TestValues.NewPathSegment();

    private static string WithUpperCaseDomain(string emailAddress)
    {
        var atIndex = emailAddress.IndexOf('@', StringComparison.Ordinal);
        return emailAddress[..atIndex] + emailAddress[atIndex..].ToUpperInvariant();
    }

    private static string ExpectedHash(string address) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(address.ToLowerInvariant()))).ToLowerInvariant();
}
