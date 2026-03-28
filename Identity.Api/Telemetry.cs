namespace Identity;

using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// OpenTelemetry instrumentation for Identity UI page events.
/// Emits counters via the <c>"Identity"</c> meter, which is registered with the Azure Monitor
/// OpenTelemetry pipeline in <c>Program.cs</c>.
/// Duende IdentityServer's built-in metrics (token issuance, introspection, client validation)
/// are emitted under the <c>"Duende.IdentityServer"</c> meter and are also registered there.
/// </summary>
public static class Telemetry
{
    public static readonly ActivitySource ActivitySource = new(nameof(Identity), "1.0.0");

    private static readonly Meter Meter = new(nameof(Identity), "1.0.0");

    /// <summary>OpenTelemetry counter metrics emitted by IdentityServer UI pages.</summary>
    public static class Metrics
    {
        private static readonly Counter<long> ConsentGrantedCounter =
            Meter.CreateCounter<long>("identity.consent.granted", description: "Number of consent grants by users.");

        private static readonly Counter<long> ConsentDeniedCounter =
            Meter.CreateCounter<long>("identity.consent.denied", description: "Number of consent denials by users.");

        private static readonly Counter<long> GrantsRevokedCounter =
            Meter.CreateCounter<long>("identity.grants.revoked", description: "Number of client grants revoked by users.");

        /// <summary>Records a consent grant for the specified client and scopes.</summary>
        /// <param name="clientId">The client that was granted consent.</param>
        /// <param name="scopes">The scopes that were consented to.</param>
        /// <param name="remember">Whether the user chose to remember the decision.</param>
        public static void ConsentGranted(string clientId, IEnumerable<string> scopes, bool remember) =>
            ConsentGrantedCounter.Add(1, new TagList
            {
                { "client_id", clientId },
                { "remember", remember },
                { "scope_count", scopes.Count() },
            });

        /// <summary>Records a consent denial for the specified client.</summary>
        /// <param name="clientId">The client whose consent was denied.</param>
        /// <param name="scopes">The scopes that were requested but denied.</param>
        public static void ConsentDenied(string clientId, IEnumerable<string> scopes) =>
            ConsentDeniedCounter.Add(1, new TagList
            {
                { "client_id", clientId },
                { "scope_count", scopes.Count() },
            });

        /// <summary>Records a grant revocation for the specified client.</summary>
        /// <param name="clientId">The client whose grants were revoked.</param>
        public static void GrantsRevoked(string? clientId) =>
            GrantsRevokedCounter.Add(1, new TagList { { "client_id", clientId } });
    }
}
