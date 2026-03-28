namespace Identity.Tests;

using System.Diagnostics.Metrics;
using Identity;

/// <summary>Unit tests for <see cref="Telemetry.Metrics"/> counter emission.</summary>
[Trait("Category", "Unit")]
public sealed class TelemetryTests
{
    /// <summary>
    /// Verifies that ConsentGranted emits the identity.consent.granted counter with a value of 1.
    /// Input: clientId="client1", scopes=["scope1"], remember=true.
    /// Expected: measurement value == 1.
    /// </summary>
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

    /// <summary>
    /// Verifies that ConsentGranted includes the client_id tag with the provided client ID.
    /// Input: clientId="client1".
    /// Expected: tags contain key "client_id" with value "client1".
    /// </summary>
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

    /// <summary>
    /// Verifies that ConsentGranted includes the remember tag with the provided value.
    /// Input: remember=true.
    /// Expected: tags contain key "remember" with value true.
    /// </summary>
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

    /// <summary>
    /// Verifies that ConsentGranted includes the scope_count tag equal to the number of scopes provided.
    /// Input: two scopes ["scope1", "scope2"].
    /// Expected: tags contain key "scope_count" with value 2.
    /// </summary>
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

    /// <summary>
    /// Verifies that ConsentGranted with an empty scopes list produces scope_count tag of 0.
    /// Input: scopes = [] (empty).
    /// Expected: tags contain key "scope_count" with value 0.
    /// </summary>
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

    /// <summary>
    /// Verifies that calling ConsentGranted does not emit the identity.consent.denied counter.
    /// Input: call only ConsentGranted.
    /// Expected: denied counter fires 0 times; granted counter fires once.
    /// </summary>
    [Fact]
    public void ConsentGranted_DoesNotEmitConsentDeniedCounter()
    {
        // Arrange
        long grantedFired = 0;
        long deniedFired = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == nameof(Identity))
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            if (instrument.Name == "identity.consent.granted")
            {
                grantedFired += value;
            }
            else if (instrument.Name == "identity.consent.denied")
            {
                deniedFired += value;
            }
        });
        listener.Start();

        // Act
        Telemetry.Metrics.ConsentGranted("client1", ["scope1"], remember: false);

        // Assert
        Assert.Equal(1, grantedFired);
        Assert.Equal(0, deniedFired);
    }

    /// <summary>
    /// Verifies that ConsentDenied emits the identity.consent.denied counter with a value of 1.
    /// Input: clientId="client2", scopes=["openid"].
    /// Expected: measurement value == 1.
    /// </summary>
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

    /// <summary>
    /// Verifies that ConsentDenied includes the client_id tag with the provided client ID.
    /// Input: clientId="client2".
    /// Expected: tags contain key "client_id" with value "client2".
    /// </summary>
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

    /// <summary>
    /// Verifies that ConsentDenied includes the scope_count tag equal to the number of scopes provided.
    /// Input: three scopes ["openid", "profile", "email"].
    /// Expected: tags contain key "scope_count" with value 3.
    /// </summary>
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

    /// <summary>
    /// Verifies that ConsentDenied with an empty scopes list produces scope_count tag of 0.
    /// Input: scopes = [] (empty).
    /// Expected: tags contain key "scope_count" with value 0.
    /// </summary>
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

    /// <summary>
    /// Verifies that GrantsRevoked emits the identity.grants.revoked counter with a value of 1.
    /// Input: clientId="client3".
    /// Expected: measurement value == 1.
    /// </summary>
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

    /// <summary>
    /// Verifies that GrantsRevoked includes the client_id tag with the provided client ID.
    /// Input: clientId="client3".
    /// Expected: tags contain key "client_id" with value "client3".
    /// </summary>
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

    /// <summary>
    /// Verifies that GrantsRevoked with a null clientId still emits the counter and includes
    /// a client_id tag with a null value.
    /// Input: clientId = null.
    /// Expected: counter fires (value == 1); tag "client_id" is present with null value.
    /// </summary>
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

    /// <summary>
    /// Creates a MeterListener that enables measurement events for the Identity meter
    /// and invokes the provided callback for a specific instrument name.
    /// </summary>
    /// <param name="instrumentName">The counter name to listen for.</param>
    /// <param name="onMeasurement">Callback invoked with the measured value and captured tags array.</param>
    /// <returns>A started <see cref="MeterListener"/> that must be disposed by the caller.</returns>
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
