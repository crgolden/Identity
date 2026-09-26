namespace Identity;

public sealed record TelemetryOptions(
    string ConsentGrantedDescription,
    string ConsentDeniedDescription,
    string GrantsRevokedDescription,
    string ExceptionDescription,
    string PasskeySignInDescription);
