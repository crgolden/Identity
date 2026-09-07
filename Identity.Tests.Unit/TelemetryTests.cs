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
            "identity.consent.granted",
            (value, _) => captured = value);

        // Act
        Telemetry.Metrics.ConsentGranted("client1", ["scope1"], remember: true);

        // Assert
        Assert.Equal(1, captured);
    }

    [Fact]
    public void ConsentGranted_TagsContainClientId()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            "identity.consent.granted",
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.ConsentGranted("client1", ["scope1"], remember: true);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, "client_id", StringComparison.Ordinal) && "client1".Equals(t.Value));
    }

    [Fact]
    public void ConsentGranted_TagsContainRemember()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            "identity.consent.granted",
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.ConsentGranted("client1", ["scope1"], remember: true);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, "remember", StringComparison.Ordinal) && true.Equals(t.Value));
    }

    [Fact]
    public void ConsentGranted_TagsContainScopeCount()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            "identity.consent.granted",
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.ConsentGranted("client1", ["scope1", "scope2"], remember: false);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, "scope_count", StringComparison.Ordinal) && 2.Equals(t.Value));
    }

    [Fact]
    public void ConsentGranted_EmptyScopes_ScopeCountTagIsZero()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            "identity.consent.granted",
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.ConsentGranted("client1", [], remember: false);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, "scope_count", StringComparison.Ordinal) && 0.Equals(t.Value));
    }

    [Fact]
    public void ConsentGranted_DoesNotEmitConsentDeniedCounter()
    {
        // Arrange
        long grantedFired = 0;
        long deniedFired = 0;
        using var grantedListener = MakeListener("identity.consent.granted", (value, _) => grantedFired += value);
        using var deniedListener = MakeListener("identity.consent.denied", (value, _) => deniedFired += value);

        // Act
        Telemetry.Metrics.ConsentGranted("client1", ["scope1"], remember: false);

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
            "identity.consent.denied",
            (value, _) => captured = value);

        // Act
        Telemetry.Metrics.ConsentDenied("client2", ["openid"]);

        // Assert
        Assert.Equal(1, captured);
    }

    [Fact]
    public void ConsentDenied_TagsContainClientId()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            "identity.consent.denied",
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.ConsentDenied("client2", ["openid"]);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, "client_id", StringComparison.Ordinal) && "client2".Equals(t.Value));
    }

    [Fact]
    public void ConsentDenied_TagsContainScopeCount()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            "identity.consent.denied",
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.ConsentDenied("client2", ["openid", "profile", "email"]);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, "scope_count", StringComparison.Ordinal) && 3.Equals(t.Value));
    }

    [Fact]
    public void ConsentDenied_EmptyScopes_ScopeCountTagIsZero()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            "identity.consent.denied",
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.ConsentDenied("client2", []);

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, "scope_count", StringComparison.Ordinal) && 0.Equals(t.Value));
    }

    [Fact]
    public void GrantsRevoked_EmitsCounterWithValueOne()
    {
        // Arrange
        long captured = 0;
        using var listener = MakeListener(
            "identity.grants.revoked",
            (value, _) => captured = value);

        // Act
        Telemetry.Metrics.GrantsRevoked("client3");

        // Assert
        Assert.Equal(1, captured);
    }

    [Fact]
    public void GrantsRevoked_TagsContainClientId()
    {
        // Arrange
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            "identity.grants.revoked",
            (_, tags) => capturedTags = tags);

        // Act
        Telemetry.Metrics.GrantsRevoked("client3");

        // Assert
        Assert.Contains(capturedTags, t => string.Equals(t.Key, "client_id", StringComparison.Ordinal) && "client3".Equals(t.Value));
    }

    [Fact]
    public void GrantsRevoked_NullClientId_EmitsCounterWithNullClientIdTag()
    {
        // Arrange
        long captured = 0;
        KeyValuePair<string, object?>[] capturedTags = [];
        using var listener = MakeListener(
            "identity.grants.revoked",
            (value, tags) =>
            {
                captured = value;
                capturedTags = tags;
            });

        // Act
        Telemetry.Metrics.GrantsRevoked(null);

        // Assert
        Assert.Equal(1, captured);
        Assert.Contains(capturedTags, t => string.Equals(t.Key, "client_id", StringComparison.Ordinal) && t.Value is null);
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
    [InlineData(true, "true")]
    [InlineData(false, "false")]
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
        Assert.Contains(capturedTags, t => string.Equals(t.Key, "succeeded", StringComparison.Ordinal) && expectedLabel.Equals(t.Value));
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
        Assert.Contains(capturedTags, t => string.Equals(t.Key, "synthetic", StringComparison.Ordinal) && "true".Equals(t.Value));
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
            t => string.Equals(t.Key, "synthetic", StringComparison.Ordinal) && "false".Equals(t.Value));
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
        Assert.Contains(capturedTags, t => string.Equals(t.Key, "synthetic", StringComparison.Ordinal) && "false".Equals(t.Value));
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