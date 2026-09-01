namespace Identity;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;

public static class Telemetry
{
    public static readonly ActivitySource ActivitySource = new(nameof(Identity), "1.0.0");

    private static readonly Meter Meter = new(nameof(Identity), "1.0.0");

    public static Activity? StartActivity(string name) =>
        ActivitySource.StartActivity(name, ActivityKind.Internal, parentContext: default);

    public static class Metrics
    {
        internal const string ExceptionCounterName = "identity.exceptions";

        internal const string ExceptionTypeTagName = "exception.type";

        internal const string SyntheticCaptchaObservedCounterName = "identity.captcha.synthetic_observed";

        internal const string SyntheticSpanTagName = "captcha.synthetic";

        internal const string ScoreSpanTagName = "captcha.score";

        private static readonly Counter<long> ConsentGrantedCounter =
            Meter.CreateCounter<long>("identity.consent.granted", description: "Number of consent grants by users.");

        private static readonly Counter<long> ConsentDeniedCounter =
            Meter.CreateCounter<long>("identity.consent.denied", description: "Number of consent denials by users.");

        private static readonly Counter<long> GrantsRevokedCounter =
            Meter.CreateCounter<long>("identity.grants.revoked", description: "Number of client grants revoked by users.");

        private static readonly Counter<long> ExceptionCounter =
            Meter.CreateCounter<long>(ExceptionCounterName, description: "Number of unhandled exceptions.");

        private static readonly Counter<long> SyntheticCaptchaObservedCounter =
            Meter.CreateCounter<long>(
                SyntheticCaptchaObservedCounterName,
                description: "Number of synthetic-marker requests whose reCAPTCHA score was observed but not enforced.");

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
            ExceptionCounter.Add(1, new TagList { { ExceptionTypeTagName, exceptionType } });

        public static void SyntheticCaptchaObserved(string action, decimal score)
        {
            Activity.Current?.SetTag(SyntheticSpanTagName, true);
            Activity.Current?.SetTag(ScoreSpanTagName, score);
            SyntheticCaptchaObservedCounter.Add(1, new TagList
            {
                { "action", action },
                { "score_bucket", score.ToString("0.0", CultureInfo.InvariantCulture) },
            });
        }
    }
}