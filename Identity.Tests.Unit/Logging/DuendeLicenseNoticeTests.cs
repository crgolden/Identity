namespace Identity.Tests.Unit.Logging;

using global::Identity.Logging;
using Infrastructure;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DuendeLicenseNoticeTests
{
    private const string LicenseValidatorSourceContext = "Duende.Private.Licencing.V2.LicenseValidator";
    private const int NoValidLicenseKeyEventId = 263521618;
    private const int FeatureUsedNoLicenseEventId = 1549918610;
    private const int QuantizedNoLicenseEventId = 1746542900;
    private const int ErrorValidatingV2LicenseKeyEventId = 2133976702;
    private const int LicenseExpiredEventId = 244747909;
    private const int FeatureNotLicensedEventId = 554619973;
    private const int QuantizedExceedsGraceEventId = 1919810387;

    [Fact]
    public void IsNoLicenseConfiguredNotice_DropsTheUnlicensedNotice()
    {
        // Arrange
        var eventId = new EventId(NoValidLicenseKeyEventId, "NoValidLicenseKey");

        // Act
        var reachedTheSink = WriteThroughFilter(LicenseValidatorSourceContext, eventId, LogLevel.Error);

        // Assert
        Assert.Empty(reachedTheSink);
    }

    [Fact]
    public void IsNoLicenseConfiguredNotice_DropsTheUnlicensedFeatureWarningThatNamesPar()
    {
        // Arrange
        var eventId = new EventId(FeatureUsedNoLicenseEventId, "FeatureUsedNoLicense");

        // Act
        var reachedTheSink = WriteThroughFilter(LicenseValidatorSourceContext, eventId, LogLevel.Warning);

        // Assert
        Assert.Empty(reachedTheSink);
    }

    [Fact]
    public void IsNoLicenseConfiguredNotice_DropsTheUnlicensedEntitlementCountWarning()
    {
        // Arrange
        var eventId = new EventId(QuantizedNoLicenseEventId, "QuantizedNoLicense");

        // Act
        var reachedTheSink = WriteThroughFilter(LicenseValidatorSourceContext, eventId, LogLevel.Warning);

        // Assert
        Assert.Empty(reachedTheSink);
    }

    [Fact]
    public void IsNoLicenseConfiguredNotice_KeepsTheMalformedLicenseKeyEventFromTheSameSource()
    {
        // Arrange
        var eventId = new EventId(ErrorValidatingV2LicenseKeyEventId, "ErrorValidatingV2LicenseKey");

        // Act
        var reachedTheSink = WriteThroughFilter(LicenseValidatorSourceContext, eventId, LogLevel.Critical);

        // Assert
        Assert.Single(reachedTheSink);
    }

    [Fact]
    public void IsNoLicenseConfiguredNotice_KeepsTheExpiredLicenseEventFromTheSameSource()
    {
        // Arrange
        var eventId = new EventId(LicenseExpiredEventId, "LicenseExpired");

        // Act
        var reachedTheSink = WriteThroughFilter(LicenseValidatorSourceContext, eventId, LogLevel.Error);

        // Assert
        Assert.Single(reachedTheSink);
    }

    [Fact]
    public void IsNoLicenseConfiguredNotice_KeepsAFeatureMissingFromAConfiguredLicense()
    {
        // Arrange
        var eventId = new EventId(FeatureNotLicensedEventId, "FeatureNotLicensed");

        // Act
        var reachedTheSink = WriteThroughFilter(LicenseValidatorSourceContext, eventId, LogLevel.Warning);

        // Assert
        Assert.Single(reachedTheSink);
    }

    [Fact]
    public void IsNoLicenseConfiguredNotice_KeepsAnEntitlementBeyondItsLicensedGrace()
    {
        // Arrange
        var eventId = new EventId(QuantizedExceedsGraceEventId, "QuantizedExceedsGrace");

        // Act
        var reachedTheSink = WriteThroughFilter(LicenseValidatorSourceContext, eventId, LogLevel.Error);

        // Assert
        Assert.Single(reachedTheSink);
    }

    [Fact]
    public void IsNoLicenseConfiguredNotice_KeepsADroppedEventNameWhenItComesFromAnotherSource()
    {
        // Arrange
        var sourceContext = $"Contoso.Licensing.{Guid.NewGuid():N}";
        var eventId = new EventId(NoValidLicenseKeyEventId, "NoValidLicenseKey");

        // Act
        var reachedTheSink = WriteThroughFilter(sourceContext, eventId, LogLevel.Error);

        // Assert
        Assert.Single(reachedTheSink);
    }

    [Fact]
    public void IsNoLicenseConfiguredNotice_KeepsAnotherEventCarryingADroppedEventIdentifier()
    {
        // Arrange
        var eventId = new EventId(NoValidLicenseKeyEventId, $"Event{Guid.NewGuid():N}");

        // Act
        var reachedTheSink = WriteThroughFilter(LicenseValidatorSourceContext, eventId, LogLevel.Error);

        // Assert
        Assert.Single(reachedTheSink);
    }

    private static IReadOnlyList<LogEvent> WriteThroughFilter(string sourceContext, EventId eventId, LogLevel logLevel)
    {
        var sink = new CapturingSink();
        using (var serilogLogger = new LoggerConfiguration()
                   .MinimumLevel.Verbose()
                   .Filter.ByExcluding(DuendeLicenseNotice.IsNoLicenseConfiguredNotice)
                   .WriteTo.Sink(sink)
                   .CreateLogger())
        using (var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
                   .SetMinimumLevel(LogLevel.Trace)
                   .AddSerilog(serilogLogger)))
        {
            loggerFactory
                .CreateLogger(sourceContext)
                .Log(logLevel, eventId, "Please start a conversation with us: https://duende.link/l/contact");
        }

        return sink.Events;
    }

    private sealed class CapturingSink : ILogEventSink
    {
        private readonly List<LogEvent> _events = [];

        public IReadOnlyList<LogEvent> Events => _events;

        public void Emit(LogEvent logEvent) => _events.Add(logEvent);
    }
}
