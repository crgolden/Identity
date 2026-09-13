namespace Identity.Tests.Unit.Logging;

internal static class DuendeLicenseEventConstants
{
    internal const int NoValidLicenseKeyEventId = 263521618;
    internal const string ErrorValidatingV2LicenseKeyEventName = "ErrorValidatingV2LicenseKey";
    internal const string LicenseExpiredEventName = "LicenseExpired";
    internal const string FeatureNotLicensedEventName = "FeatureNotLicensed";
    internal const string QuantizedExceedsLimitEventName = "QuantizedExceedsLimit";
    internal const string QuantizedExceedsGraceEventName = "QuantizedExceedsGrace";
    internal const string LicenseValidUntilEventName = "LicenseValidUntil";
    internal const string LicenseValidEventName = "LicenseValid";
}
