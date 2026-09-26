namespace Identity.Tests.Unit;

using Identity;
using Identity.Tests.Unit.Infrastructure;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class TelemetryTests : IDisposable
{
    private readonly TelemetryHarness _harness = new();

    [Fact]
    public void Counters_CarryTheConfiguredDescriptions()
    {
        // Arrange
        Dictionary<string, string?> expected = new(StringComparer.Ordinal)
        {
            [Telemetry.Metrics.ConsentGrantedCounterName] = _harness.Descriptions.ConsentGrantedDescription,
            [Telemetry.Metrics.ConsentDeniedCounterName] = _harness.Descriptions.ConsentDeniedDescription,
            [Telemetry.Metrics.GrantsRevokedCounterName] = _harness.Descriptions.GrantsRevokedDescription,
            [Telemetry.Metrics.ExceptionCounterName] = _harness.Descriptions.ExceptionDescription,
            [Telemetry.Metrics.PasskeySignInCounterName] = _harness.Descriptions.PasskeySignInDescription,
        };

        // Act
        var actual = _harness.PublishedDescriptions();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConsentGranted_EmitsCounterWithValueOne()
    {
        // Arrange
        long captured = 0;
        using var listener = _harness.ListenTo(
            Telemetry.Metrics.ConsentGrantedCounterName,
            (value, _) => captured = value);

        // Act
        _harness.Telemetry.ConsentGranted(Generated.NewClientIdentifier(), [Generated.NewScopeName()], remember: true);

        // Assert
        Assert.Equal(1, captured);
    }

    [Fact]
    public void ConsentGranted_TagsContainClientId()
    {
        // Arrange
        var grantedClientId = Generated.NewClientIdentifier();
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = _harness.ListenTo(
            Telemetry.Metrics.ConsentGrantedCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        _harness.Telemetry.ConsentGranted(grantedClientId, [Generated.NewScopeName()], remember: true);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.ClientIdTagName, StringComparison.Ordinal) && grantedClientId.Equals(t.Value));
    }

    [Fact]
    public void ConsentGranted_TagsContainRemember()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = _harness.ListenTo(
            Telemetry.Metrics.ConsentGrantedCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        _harness.Telemetry.ConsentGranted(Generated.NewClientIdentifier(), [Generated.NewScopeName()], remember: true);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.RememberTagName, StringComparison.Ordinal) && true.Equals(t.Value));
    }

    [Fact]
    public void ConsentGranted_TagsContainScopeCount()
    {
        // Arrange
        string[] grantedScopes = [Generated.NewScopeName(), Generated.NewScopeName()];
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = _harness.ListenTo(
            Telemetry.Metrics.ConsentGrantedCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        _harness.Telemetry.ConsentGranted(Generated.NewClientIdentifier(), grantedScopes, remember: false);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.ScopeCountTagName, StringComparison.Ordinal) && grantedScopes.Length.Equals(t.Value));
    }

    [Fact]
    public void ConsentGranted_EmptyScopes_ScopeCountTagIsZero()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = _harness.ListenTo(
            Telemetry.Metrics.ConsentGrantedCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        _harness.Telemetry.ConsentGranted(Generated.NewClientIdentifier(), [], remember: false);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.ScopeCountTagName, StringComparison.Ordinal) && 0.Equals(t.Value));
    }

    [Fact]
    public void ConsentGranted_DoesNotEmitConsentDeniedCounter()
    {
        // Arrange
        long grantedFired = 0;
        long deniedFired = 0;
        using var grantedListener = _harness.ListenTo(
            Telemetry.Metrics.ConsentGrantedCounterName,
            (value, _) => grantedFired += value);
        using var deniedListener = _harness.ListenTo(
            Telemetry.Metrics.ConsentDeniedCounterName,
            (value, _) => deniedFired += value);

        // Act
        _harness.Telemetry.ConsentGranted(Generated.NewClientIdentifier(), [Generated.NewScopeName()], remember: false);

        // Assert
        Assert.Equal(1, grantedFired);
        Assert.Equal(0, deniedFired);
    }

    [Fact]
    public void ConsentDenied_EmitsCounterWithValueOne()
    {
        // Arrange
        long captured = 0;
        using var listener = _harness.ListenTo(
            Telemetry.Metrics.ConsentDeniedCounterName,
            (value, _) => captured = value);

        // Act
        _harness.Telemetry.ConsentDenied(Generated.NewClientIdentifier(), [Generated.NewScopeName()]);

        // Assert
        Assert.Equal(1, captured);
    }

    [Fact]
    public void ConsentDenied_TagsContainClientId()
    {
        // Arrange
        var deniedClientId = Generated.NewClientIdentifier();
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = _harness.ListenTo(
            Telemetry.Metrics.ConsentDeniedCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        _harness.Telemetry.ConsentDenied(deniedClientId, [Generated.NewScopeName()]);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.ClientIdTagName, StringComparison.Ordinal) && deniedClientId.Equals(t.Value));
    }

    [Fact]
    public void ConsentDenied_TagsContainScopeCount()
    {
        // Arrange
        string[] deniedScopes = [Generated.NewScopeName(), Generated.NewScopeName(), Generated.NewScopeName()];
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = _harness.ListenTo(
            Telemetry.Metrics.ConsentDeniedCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        _harness.Telemetry.ConsentDenied(Generated.NewClientIdentifier(), deniedScopes);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.ScopeCountTagName, StringComparison.Ordinal) && deniedScopes.Length.Equals(t.Value));
    }

    [Fact]
    public void ConsentDenied_EmptyScopes_ScopeCountTagIsZero()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = _harness.ListenTo(
            Telemetry.Metrics.ConsentDeniedCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        _harness.Telemetry.ConsentDenied(Generated.NewClientIdentifier(), []);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.ScopeCountTagName, StringComparison.Ordinal) && 0.Equals(t.Value));
    }

    [Fact]
    public void GrantsRevoked_EmitsCounterWithValueOne()
    {
        // Arrange
        long captured = 0;
        using var listener = _harness.ListenTo(
            Telemetry.Metrics.GrantsRevokedCounterName,
            (value, _) => captured = value);

        // Act
        _harness.Telemetry.GrantsRevoked(Generated.NewClientIdentifier());

        // Assert
        Assert.Equal(1, captured);
    }

    [Fact]
    public void GrantsRevoked_TagsContainClientId()
    {
        // Arrange
        var revokedClientId = Generated.NewClientIdentifier();
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = _harness.ListenTo(
            Telemetry.Metrics.GrantsRevokedCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        _harness.Telemetry.GrantsRevoked(revokedClientId);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.ClientIdTagName, StringComparison.Ordinal) && revokedClientId.Equals(t.Value));
    }

    [Fact]
    public void GrantsRevoked_NullClientId_EmitsCounterWithNullClientIdTag()
    {
        // Arrange
        long captured = 0;
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = _harness.ListenTo(
            Telemetry.Metrics.GrantsRevokedCounterName,
            (value, tags) =>
            {
                captured = value;
                capturedTags = tags;
            });

        // Act
        _harness.Telemetry.GrantsRevoked(null);

        // Assert
        Assert.Equal(1, captured);
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.ClientIdTagName, StringComparison.Ordinal) && t.Value is null);
    }

    [Fact]
    public void PasskeySignInCounterName_IsTheNameTheWalkerDashboardQueries()
    {
        // Act
        var counterName = Telemetry.Metrics.PasskeySignInCounterName;

        // Assert
        Assert.Equal("identity.login.passkey_signins", counterName);
    }

    [Fact]
    public void SyntheticUserAgentToken_IsTheSuffixTheWalkersSend()
    {
        // Act
        var token = Telemetry.Metrics.SyntheticUserAgentToken;

        // Assert
        Assert.Equal("crgolden-synthetic", token);
    }

    [Fact]
    public void MetricTagNames_AreTheNamesTheDashboardGroupsAndFiltersBy()
    {
        // Arrange
        string[] expected = ["client_id", "remember", "scope_count", "succeeded", "synthetic", "true", "false"];

        // Act
        string[] actual =
        [
            Telemetry.Metrics.ClientIdTagName,
            Telemetry.Metrics.RememberTagName,
            Telemetry.Metrics.ScopeCountTagName,
            Telemetry.Metrics.SucceededTagName,
            Telemetry.Metrics.SyntheticTagName,
            Telemetry.Metrics.TrueLabel,
            Telemetry.Metrics.FalseLabel,
        ];

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConsentAndGrantCounterNames_AreTheNamesTheDashboardQueries()
    {
        // Arrange
        string[] expected =
        [
            "identity.consent.granted",
            "identity.consent.denied",
            "identity.grants.revoked",
            "identity.exceptions",
            "exception.type",
        ];

        // Act
        string[] actual =
        [
            Telemetry.Metrics.ConsentGrantedCounterName,
            Telemetry.Metrics.ConsentDeniedCounterName,
            Telemetry.Metrics.GrantsRevokedCounterName,
            Telemetry.Metrics.ExceptionCounterName,
            Telemetry.Metrics.ExceptionTypeTagName,
        ];

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PasskeySignIn_EmitsCounterWithValueOne()
    {
        // Arrange
        long captured = 0;
        using var listener = _harness.ListenTo(
            Telemetry.Metrics.PasskeySignInCounterName,
            (value, _) => captured = value);

        // Act
        _harness.Telemetry.PasskeySignIn(succeeded: true, Generated.NewBrowserUserAgent());

        // Assert
        Assert.Equal(1, captured);
    }

    [Theory]
    [InlineData(true, Telemetry.Metrics.TrueLabel)]
    [InlineData(false, Telemetry.Metrics.FalseLabel)]
    public void PasskeySignIn_TagsContainOutcome(bool succeeded, string expectedLabel)
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = _harness.ListenTo(
            Telemetry.Metrics.PasskeySignInCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        _harness.Telemetry.PasskeySignIn(succeeded, Generated.NewBrowserUserAgent());

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.SucceededTagName, StringComparison.Ordinal) && expectedLabel.Equals(t.Value));
    }

    [Fact]
    public void PasskeySignIn_WalkerUserAgent_TagsSyntheticTrue()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = _harness.ListenTo(
            Telemetry.Metrics.PasskeySignInCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        _harness.Telemetry.PasskeySignIn(succeeded: true, Generated.NewUserAgentTaggedWith(Telemetry.Metrics.SyntheticUserAgentToken));

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.SyntheticTagName, StringComparison.Ordinal) && Telemetry.Metrics.TrueLabel.Equals(t.Value));
    }

    [Fact]
    public void PasskeySignIn_OrdinaryBrowserUserAgent_TagsSyntheticFalse()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = _harness.ListenTo(
            Telemetry.Metrics.PasskeySignInCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        _harness.Telemetry.PasskeySignIn(succeeded: true, Generated.NewBrowserUserAgent());

        // Assert
        Assert.Contains(
            capturedTags,
            t => string.Equals(t.Key, Telemetry.Metrics.SyntheticTagName, StringComparison.Ordinal) && Telemetry.Metrics.FalseLabel.Equals(t.Value));
    }

    [Fact]
    public void PasskeySignIn_NullUserAgent_TagsSyntheticFalse()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = _harness.ListenTo(
            Telemetry.Metrics.PasskeySignInCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        _harness.Telemetry.PasskeySignIn(succeeded: false, userAgent: null);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.SyntheticTagName, StringComparison.Ordinal) && Telemetry.Metrics.FalseLabel.Equals(t.Value));
    }

    public void Dispose() => _harness.Dispose();
}
