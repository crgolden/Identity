namespace Identity.Tests.Unit;

using System.Diagnostics.Metrics;
using Identity;
using Infrastructure;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class TelemetryTests
{
    [Fact]
    public void ConsentGranted_EmitsCounterWithValueOne()
    {
        // Arrange
        long captured = 0;
        using var listener = MakeListener(
            Telemetry.Metrics.ConsentGrantedCounterName,
            (value, _) => captured = value);

        // Act
        Telemetry.Metrics.ConsentGranted(TestValues.NewClientIdentifier(), [TestValues.NewScopeName()], remember: true);

        // Assert
        Assert.Equal(1, captured);
    }

    [Fact]
    public void ConsentGranted_TagsContainClientId()
    {
        // Arrange
        var grantedClientId = TestValues.NewClientIdentifier();
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            Telemetry.Metrics.ConsentGrantedCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.ConsentGranted(grantedClientId, [TestValues.NewScopeName()], remember: true);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.ClientIdTagName, StringComparison.Ordinal) && grantedClientId.Equals(t.Value));
    }

    [Fact]
    public void ConsentGranted_TagsContainRemember()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            Telemetry.Metrics.ConsentGrantedCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.ConsentGranted(TestValues.NewClientIdentifier(), [TestValues.NewScopeName()], remember: true);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.RememberTagName, StringComparison.Ordinal) && true.Equals(t.Value));
    }

    [Fact]
    public void ConsentGranted_TagsContainScopeCount()
    {
        // Arrange
        string[] grantedScopes = [TestValues.NewScopeName(), TestValues.NewScopeName()];
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            Telemetry.Metrics.ConsentGrantedCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.ConsentGranted(TestValues.NewClientIdentifier(), grantedScopes, remember: false);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.ScopeCountTagName, StringComparison.Ordinal) && grantedScopes.Length.Equals(t.Value));
    }

    [Fact]
    public void ConsentGranted_EmptyScopes_ScopeCountTagIsZero()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            Telemetry.Metrics.ConsentGrantedCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.ConsentGranted(TestValues.NewClientIdentifier(), [], remember: false);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.ScopeCountTagName, StringComparison.Ordinal) && 0.Equals(t.Value));
    }

    [Fact]
    public void ConsentGranted_DoesNotEmitConsentDeniedCounter()
    {
        // Arrange
        long grantedFired = 0;
        long deniedFired = 0;
        using var grantedListener = MakeListener(
            Telemetry.Metrics.ConsentGrantedCounterName,
            (value, _) => grantedFired += value);
        using var deniedListener = MakeListener(
            Telemetry.Metrics.ConsentDeniedCounterName,
            (value, _) => deniedFired += value);

        // Act
        Telemetry.Metrics.ConsentGranted(TestValues.NewClientIdentifier(), [TestValues.NewScopeName()], remember: false);

        // Assert
        Assert.Equal(1, grantedFired);
        Assert.Equal(0, deniedFired);
    }

    [Fact]
    public void ConsentDenied_EmitsCounterWithValueOne()
    {
        // Arrange
        long captured = 0;
        using var listener = MakeListener(
            Telemetry.Metrics.ConsentDeniedCounterName,
            (value, _) => captured = value);

        // Act
        Telemetry.Metrics.ConsentDenied(TestValues.NewClientIdentifier(), [TestValues.NewScopeName()]);

        // Assert
        Assert.Equal(1, captured);
    }

    [Fact]
    public void ConsentDenied_TagsContainClientId()
    {
        // Arrange
        var deniedClientId = TestValues.NewClientIdentifier();
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            Telemetry.Metrics.ConsentDeniedCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.ConsentDenied(deniedClientId, [TestValues.NewScopeName()]);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.ClientIdTagName, StringComparison.Ordinal) && deniedClientId.Equals(t.Value));
    }

    [Fact]
    public void ConsentDenied_TagsContainScopeCount()
    {
        // Arrange
        string[] deniedScopes = [TestValues.NewScopeName(), TestValues.NewScopeName(), TestValues.NewScopeName()];
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            Telemetry.Metrics.ConsentDeniedCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.ConsentDenied(TestValues.NewClientIdentifier(), deniedScopes);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.ScopeCountTagName, StringComparison.Ordinal) && deniedScopes.Length.Equals(t.Value));
    }

    [Fact]
    public void ConsentDenied_EmptyScopes_ScopeCountTagIsZero()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            Telemetry.Metrics.ConsentDeniedCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.ConsentDenied(TestValues.NewClientIdentifier(), []);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.ScopeCountTagName, StringComparison.Ordinal) && 0.Equals(t.Value));
    }

    [Fact]
    public void GrantsRevoked_EmitsCounterWithValueOne()
    {
        // Arrange
        long captured = 0;
        using var listener = MakeListener(
            Telemetry.Metrics.GrantsRevokedCounterName,
            (value, _) => captured = value);

        // Act
        Telemetry.Metrics.GrantsRevoked(TestValues.NewClientIdentifier());

        // Assert
        Assert.Equal(1, captured);
    }

    [Fact]
    public void GrantsRevoked_TagsContainClientId()
    {
        // Arrange
        var revokedClientId = TestValues.NewClientIdentifier();
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            Telemetry.Metrics.GrantsRevokedCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.GrantsRevoked(revokedClientId);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.ClientIdTagName, StringComparison.Ordinal) && revokedClientId.Equals(t.Value));
    }

    [Fact]
    public void GrantsRevoked_NullClientId_EmitsCounterWithNullClientIdTag()
    {
        // Arrange
        long captured = 0;
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            Telemetry.Metrics.GrantsRevokedCounterName,
            (value, tags) =>
            {
                captured = value;
                capturedTags = tags;
            });

        // Act
        Telemetry.Metrics.GrantsRevoked(null);

        // Assert
        Assert.Equal(1, captured);
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.ClientIdTagName, StringComparison.Ordinal) && t.Value is null);
    }

    [Fact]
    public void PasskeySignInCounterName_IsTheNameTheWalkerDashboardQueries()
    {
        Assert.Equal("identity.login.passkey_signins", Telemetry.Metrics.PasskeySignInCounterName);
    }

    [Fact]
    public void SyntheticUserAgentToken_IsTheSuffixTheWalkersSend()
    {
        Assert.Equal("crgolden-synthetic", Telemetry.Metrics.SyntheticUserAgentToken);
    }

    [Fact]
    public void MetricTagNames_AreTheNamesTheDashboardGroupsAndFiltersBy()
    {
        Assert.Equal("client_id", Telemetry.Metrics.ClientIdTagName);
        Assert.Equal("remember", Telemetry.Metrics.RememberTagName);
        Assert.Equal("scope_count", Telemetry.Metrics.ScopeCountTagName);
        Assert.Equal("succeeded", Telemetry.Metrics.SucceededTagName);
        Assert.Equal("synthetic", Telemetry.Metrics.SyntheticTagName);
        Assert.Equal("true", Telemetry.Metrics.TrueLabel);
        Assert.Equal("false", Telemetry.Metrics.FalseLabel);
    }

    [Fact]
    public void ConsentAndGrantCounterNames_AreTheNamesTheDashboardQueries()
    {
        Assert.Equal("identity.consent.granted", Telemetry.Metrics.ConsentGrantedCounterName);
        Assert.Equal("identity.consent.denied", Telemetry.Metrics.ConsentDeniedCounterName);
        Assert.Equal("identity.grants.revoked", Telemetry.Metrics.GrantsRevokedCounterName);
        Assert.Equal("identity.exceptions", Telemetry.Metrics.ExceptionCounterName);
        Assert.Equal("exception.type", Telemetry.Metrics.ExceptionTypeTagName);
    }

    [Fact]
    public void PasskeySignIn_EmitsCounterWithValueOne()
    {
        // Arrange
        long captured = 0;
        using var listener = MakeListener(
            Telemetry.Metrics.PasskeySignInCounterName,
            (value, _) => captured = value);

        // Act
        Telemetry.Metrics.PasskeySignIn(succeeded: true, TestValues.NewBrowserUserAgent());

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
        using var listener = MakeListener(
            Telemetry.Metrics.PasskeySignInCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.PasskeySignIn(succeeded, TestValues.NewBrowserUserAgent());

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.SucceededTagName, StringComparison.Ordinal) && expectedLabel.Equals(t.Value));
    }

    [Fact]
    public void PasskeySignIn_WalkerUserAgent_TagsSyntheticTrue()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            Telemetry.Metrics.PasskeySignInCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.PasskeySignIn(succeeded: true, TestValues.NewSyntheticWalkerUserAgent());

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.SyntheticTagName, StringComparison.Ordinal) && Telemetry.Metrics.TrueLabel.Equals(t.Value));
    }

    [Fact]
    public void PasskeySignIn_OrdinaryBrowserUserAgent_TagsSyntheticFalse()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            Telemetry.Metrics.PasskeySignInCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.PasskeySignIn(succeeded: true, TestValues.NewBrowserUserAgent());

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
        using var listener = MakeListener(
            Telemetry.Metrics.PasskeySignInCounterName,
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.PasskeySignIn(succeeded: false, userAgent: null);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, Telemetry.Metrics.SyntheticTagName, StringComparison.Ordinal) && Telemetry.Metrics.FalseLabel.Equals(t.Value));
    }

    private static MeterListener MakeListener(
        string instrumentName,
        Action<long, KeyValuePair<string, object?>[]> onMeasurement)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == nameof(Identity))
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            if (instrument.Name == instrumentName)
            {
                onMeasurement(value, tags.ToArray());
            }
        });
        listener.Start();
        return listener;
    }
}