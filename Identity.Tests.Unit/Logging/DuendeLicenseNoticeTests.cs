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
    private const string DuendeLicenseValidatorSourceContext = "Duende.Private.Licencing.V2.LicenseValidator";
    private const int NoValidLicenseKeyEventId = 263521618;
    private const int ErrorValidatingV2LicenseKeyEventId = 2133976702;
    private const int LicenseExpiredEventId = 244747909;

    [Fact]
    public void IsUnlicensedNotice_DropsTheUnlicensedNoticeBeforeItReachesTheSink()
    {
        // Arrange
        var eventId = new EventId(NoValidLicenseKeyEventId, "NoValidLicenseKey");

        // Act
        var reachedTheSink = WriteThroughFilter(DuendeLicenseValidatorSourceContext, eventId, LogLevel.Error);

        // Assert
        Assert.Empty(reachedTheSink);
    }

    [Fact]
    public void IsUnlicensedNotice_KeepsTheMalformedLicenseKeyEventFromTheSameSource()
    {
        // Arrange
        var eventId = new EventId(ErrorValidatingV2LicenseKeyEventId, "ErrorValidatingV2LicenseKey");

        // Act
        var reachedTheSink = WriteThroughFilter(DuendeLicenseValidatorSourceContext, eventId, LogLevel.Critical);

        // Assert
        Assert.Single(reachedTheSink);
    }

    [Fact]
    public void IsUnlicensedNotice_KeepsTheExpiredLicenseEventFromTheSameSource()
    {
        // Arrange
        var eventId = new EventId(LicenseExpiredEventId, "LicenseExpired");

        // Act
        var reachedTheSink = WriteThroughFilter(DuendeLicenseValidatorSourceContext, eventId, LogLevel.Error);

        // Assert
        Assert.Single(reachedTheSink);
    }

    [Fact]
    public void IsUnlicensedNotice_KeepsTheUnlicensedNoticeNameWhenItComesFromAnotherSource()
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
    public void IsUnlicensedNotice_KeepsAnotherEventCarryingTheUnlicensedNoticeIdentifier()
    {
        // Arrange
        var eventId = new EventId(NoValidLicenseKeyEventId, $"Event{Guid.NewGuid():N}");

        // Act
        var reachedTheSink = WriteThroughFilter(DuendeLicenseValidatorSourceContext, eventId, LogLevel.Error);

        // Assert
        Assert.Single(reachedTheSink);
    }

    private static IReadOnlyList<LogEvent> WriteThroughFilter(string sourceContext, EventId eventId, LogLevel logLevel)
    {
        var sink = new CapturingSink();
        using (var serilogLogger = new LoggerConfiguration()
                   .MinimumLevel.Verbose()
                   .Filter.ByExcluding(DuendeLicenseNotice.IsUnlicensedNotice)
                   .WriteTo.Sink(sink)
                   .CreateLogger())
        using (var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
                   .SetMinimumLevel(LogLevel.Trace)
                   .AddSerilog(serilogLogger)))
        {
            loggerFactory
                .CreateLogger(sourceContext)
                .Log(logLevel, eventId, "You do not have a valid license key for the Duende software.");
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
