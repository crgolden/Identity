namespace Identity.Tests;
using Infrastructure;

using System.Diagnostics.Metrics;
using Identity;

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
        Assert.Contains(capturedTags, t => t.Key == "client_id" && "client1".Equals(t.Value));
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
        Assert.Contains(capturedTags, t => t.Key == "remember" && true.Equals(t.Value));
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
        Assert.Contains(capturedTags, t => t.Key == "scope_count" && 2.Equals(t.Value));
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
        Assert.Contains(capturedTags, t => t.Key == "scope_count" && 0.Equals(t.Value));
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
        Assert.Contains(capturedTags, t => t.Key == "client_id" && "client2".Equals(t.Value));
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
        Assert.Contains(capturedTags, t => t.Key == "scope_count" && 3.Equals(t.Value));
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
        Assert.Contains(capturedTags, t => t.Key == "scope_count" && 0.Equals(t.Value));
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
        Assert.Contains(capturedTags, t => t.Key == "client_id" && "client3".Equals(t.Value));
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
        Assert.Contains(capturedTags, t => t.Key == "client_id" && t.Value is null);
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
