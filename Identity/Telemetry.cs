namespace Identity;

using System.Diagnostics;
using System.Diagnostics.Metrics;

public static class Telemetry
{
    public static readonly ActivitySource ActivitySource = new(nameof(Identity), "1.0.0");

    private static readonly Meter Meter = new(nameof(Identity), "1.0.0");

    public static Activity? StartActivity(string name) =>
        ActivitySource.StartActivity(name, ActivityKind.Internal, parentContext: default);

    public static class Metrics
    {
        private static readonly Counter<long> ConsentGrantedCounter =
            Meter.CreateCounter<long>("identity.consent.granted", description: "Number of consent grants by users.");

        private static readonly Counter<long> ConsentDeniedCounter =
            Meter.CreateCounter<long>("identity.consent.denied", description: "Number of consent denials by users.");

        private static readonly Counter<long> GrantsRevokedCounter =
            Meter.CreateCounter<long>("identity.grants.revoked", description: "Number of client grants revoked by users.");

        private static readonly Counter<long> ExceptionCounter =
            Meter.CreateCounter<long>("identity.exceptions", description: "Number of unhandled exceptions.");

        public static void ConsentGranted(string clientId, IEnumerable<string> scopes, bool remember) =>
            ConsentGrantedCounter.Add(1, new TagList
            {
                { "client_id", clientId },
                { "remember", remember },
                { "scope_count", scopes.Count() },
            });

        public static void ConsentDenied(string clientId, IEnumerable<string> scopes) =>
            ConsentDeniedCounter.Add(1, new TagList
            {
                { "client_id", clientId },
                { "scope_count", scopes.Count() },
            });

        public static void GrantsRevoked(string? clientId) =>
            GrantsRevokedCounter.Add(1, new TagList { { "client_id", clientId } });

        public static void ExceptionOccurred(string exceptionType) =>
            ExceptionCounter.Add(1, new TagList { { "exception.type", exceptionType } });
    }
}