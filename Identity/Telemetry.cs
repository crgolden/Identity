namespace Identity;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;

public sealed class Telemetry : IDisposable
{
    public const string SourceName = nameof(Identity);

    private readonly ActivitySource _activitySource;
    private readonly Counter<long> _consentGrantedCounter;
    private readonly Counter<long> _consentDeniedCounter;
    private readonly Counter<long> _grantsRevokedCounter;
    private readonly Counter<long> _exceptionCounter;
    private readonly Counter<long> _passkeySignInCounter;

    public Telemetry(IMeterFactory meterFactory, IOptions<TelemetryOptions> telemetryOptions)
    {
        var version = typeof(Telemetry).Assembly.GetName().Version?.ToString();
        var descriptions = telemetryOptions.Value;
        var meter = meterFactory.Create(SourceName, version);
        _activitySource = new ActivitySource(SourceName, version);
        _consentGrantedCounter = meter.CreateCounter<long>(Metrics.ConsentGrantedCounterName, description: descriptions.ConsentGrantedDescription);
        _consentDeniedCounter = meter.CreateCounter<long>(Metrics.ConsentDeniedCounterName, description: descriptions.ConsentDeniedDescription);
        _grantsRevokedCounter = meter.CreateCounter<long>(Metrics.GrantsRevokedCounterName, description: descriptions.GrantsRevokedDescription);
        _exceptionCounter = meter.CreateCounter<long>(Metrics.ExceptionCounterName, description: descriptions.ExceptionDescription);
        _passkeySignInCounter = meter.CreateCounter<long>(Metrics.PasskeySignInCounterName, description: descriptions.PasskeySignInDescription);
    }

    public Activity? StartActivity(string name) =>
        _activitySource.StartActivity(name, ActivityKind.Internal, parentContext: default);

    public void ConsentGranted(string clientId, IEnumerable<string> scopes, bool remember) =>
        _consentGrantedCounter.Add(1, new TagList
        {
            { Metrics.ClientIdTagName, clientId },
            { Metrics.RememberTagName, remember },
            { Metrics.ScopeCountTagName, scopes.Count() },
        });

    public void ConsentDenied(string clientId, IEnumerable<string> scopes) =>
        _consentDeniedCounter.Add(1, new TagList
        {
            { Metrics.ClientIdTagName, clientId },
            { Metrics.ScopeCountTagName, scopes.Count() },
        });

    public void GrantsRevoked(string? clientId) =>
        _grantsRevokedCounter.Add(1, new TagList { { Metrics.ClientIdTagName, clientId } });

    public void ExceptionOccurred(string exceptionType) =>
        _exceptionCounter.Add(1, new TagList { { Metrics.ExceptionTypeTagName, exceptionType } });

    public void PasskeySignIn(bool succeeded, string? userAgent) =>
        _passkeySignInCounter.Add(1, new TagList
        {
            { Metrics.SucceededTagName, LabelValue(succeeded) },
            { Metrics.SyntheticTagName, LabelValue(IsSyntheticUserAgent(userAgent)) },
        });

    public void Dispose() => _activitySource.Dispose();

    private static string LabelValue(bool value) => value ? Metrics.TrueLabel : Metrics.FalseLabel;

    private static bool IsSyntheticUserAgent(string? userAgent) =>
        userAgent?.Contains(Metrics.SyntheticUserAgentToken, StringComparison.OrdinalIgnoreCase) == true;

    public static class Metrics
    {
        internal const string ExceptionCounterName = "identity.exceptions";

        internal const string ExceptionTypeTagName = "exception.type";

        internal const string ExceptionEventName = "exception";

        internal const string ExceptionMessageTagName = "exception.message";

        internal const string UnknownExceptionType = "Unknown";

        internal const string PasskeySignInCounterName = "identity.login.passkey_signins";

        internal const string SyntheticUserAgentToken = "crgolden-synthetic";

        internal const string ConsentGrantedCounterName = "identity.consent.granted";

        internal const string ConsentDeniedCounterName = "identity.consent.denied";

        internal const string GrantsRevokedCounterName = "identity.grants.revoked";

        internal const string ClientIdTagName = "client_id";

        internal const string RememberTagName = "remember";

        internal const string ScopeCountTagName = "scope_count";

        internal const string SucceededTagName = "succeeded";

        internal const string SyntheticTagName = "synthetic";

        internal const string TrueLabel = "true";

        internal const string FalseLabel = "false";
    }
}
