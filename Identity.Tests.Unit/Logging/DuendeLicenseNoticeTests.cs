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
    private const string LicenseValidatorSourceContext = DuendeLicenseNotice.LicenseValidatorSourceContext;
    private const int NoValidLicenseKeyEventId = DuendeLicenseEventConstants.NoValidLicenseKeyEventId;
    private const string ErrorValidatingV2LicenseKeyEventName = DuendeLicenseEventConstants.ErrorValidatingV2LicenseKeyEventName;
    private const string LicenseExpiredEventName = DuendeLicenseEventConstants.LicenseExpiredEventName;
    private const string FeatureNotLicensedEventName = DuendeLicenseEventConstants.FeatureNotLicensedEventName;
    private const string QuantizedExceedsLimitEventName = DuendeLicenseEventConstants.QuantizedExceedsLimitEventName;
    private const string QuantizedExceedsGraceEventName = DuendeLicenseEventConstants.QuantizedExceedsGraceEventName;
    private const string LicenseValidUntilEventName = DuendeLicenseEventConstants.LicenseValidUntilEventName;
    private const string LicenseValidEventName = DuendeLicenseEventConstants.LicenseValidEventName;

    [Fact]
    public void IsNoLicenseConfiguredNotice_DropsTheUnlicensedNotice()
    {
        // Arrange
        var eventId = new EventId(TestValues.NewEntityId(), DuendeLicenseNotice.NoValidLicenseKeyEventName);

        // Act
        var reachedTheSink = WriteThroughFilter(LicenseValidatorSourceContext, eventId, LogLevel.Error);

        // Assert
        Assert.Empty(reachedTheSink);
    }

    [Fact]
    public void IsNoLicenseConfiguredNotice_DropsTheUnlicensedFeatureWarningThatNamesPar()
    {
        // Arrange
        var eventId = new EventId(TestValues.NewEntityId(), DuendeLicenseNotice.FeatureUsedNoLicenseEventName);

        // Act
        var reachedTheSink = WriteThroughFilter(LicenseValidatorSourceContext, eventId, LogLevel.Warning);

        // Assert
        Assert.Empty(reachedTheSink);
    }

    [Fact]
    public void IsNoLicenseConfiguredNotice_DropsTheUnlicensedEntitlementCountWarning()
    {
        // Arrange
        var eventId = new EventId(TestValues.NewEntityId(), DuendeLicenseNotice.QuantizedNoLicenseEventName);

        // Act
        var reachedTheSink = WriteThroughFilter(LicenseValidatorSourceContext, eventId, LogLevel.Warning);

        // Assert
        Assert.Empty(reachedTheSink);
    }

    [Fact]
    public void IsNoLicenseConfiguredNotice_KeepsTheMalformedLicenseKeyEventFromTheSameSource()
    {
        // Arrange
        var eventId = new EventId(TestValues.NewEntityId(), ErrorValidatingV2LicenseKeyEventName);

        // Act
        var reachedTheSink = WriteThroughFilter(LicenseValidatorSourceContext, eventId, LogLevel.Critical);

        // Assert
        Assert.Single(reachedTheSink);
    }

    [Fact]
    public void IsNoLicenseConfiguredNotice_KeepsTheExpiredLicenseEventFromTheSameSource()
    {
        // Arrange
        var eventId = new EventId(TestValues.NewEntityId(), LicenseExpiredEventName);

        // Act
        var reachedTheSink = WriteThroughFilter(LicenseValidatorSourceContext, eventId, LogLevel.Error);

        // Assert
        Assert.Single(reachedTheSink);
    }

    [Fact]
    public void IsNoLicenseConfiguredNotice_KeepsAFeatureMissingFromAConfiguredLicense()
    {
        // Arrange
        var eventId = new EventId(TestValues.NewEntityId(), FeatureNotLicensedEventName);

        // Act
        var reachedTheSink = WriteThroughFilter(LicenseValidatorSourceContext, eventId, LogLevel.Warning);

        // Assert
        Assert.Single(reachedTheSink);
    }

    [Fact]
    public void IsNoLicenseConfiguredNotice_KeepsAnEntitlementBeyondItsLicensedLimit()
    {
        // Arrange
        var eventId = new EventId(TestValues.NewEntityId(), QuantizedExceedsLimitEventName);

        // Act
        var reachedTheSink = WriteThroughFilter(LicenseValidatorSourceContext, eventId, LogLevel.Warning);

        // Assert
        Assert.Single(reachedTheSink);
    }

    [Fact]
    public void IsNoLicenseConfiguredNotice_KeepsTheLicenseValidUntilNotice()
    {
        // Arrange
        var eventId = new EventId(TestValues.NewEntityId(), LicenseValidUntilEventName);

        // Act
        var reachedTheSink = WriteThroughFilter(LicenseValidatorSourceContext, eventId, LogLevel.Information);

        // Assert
        Assert.Single(reachedTheSink);
    }

    [Fact]
    public void IsNoLicenseConfiguredNotice_KeepsTheValidLicenseNotice()
    {
        // Arrange
        var eventId = new EventId(TestValues.NewEntityId(), LicenseValidEventName);

        // Act
        var reachedTheSink = WriteThroughFilter(LicenseValidatorSourceContext, eventId, LogLevel.Information);

        // Assert
        Assert.Single(reachedTheSink);
    }

    [Fact]
    public void IsNoLicenseConfiguredNotice_KeepsAnEntitlementBeyondItsLicensedGrace()
    {
        // Arrange
        var eventId = new EventId(TestValues.NewEntityId(), QuantizedExceedsGraceEventName);

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
        var eventId = new EventId(TestValues.NewEntityId(), DuendeLicenseNotice.NoValidLicenseKeyEventName);

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
                .Log(logLevel, eventId, TestValues.NewValidationMessage());
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
