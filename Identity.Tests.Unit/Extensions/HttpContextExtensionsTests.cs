namespace Identity.Tests.Unit.Extensions;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Mime;
using Identity.Extensions;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class HttpContextExtensionsTests : IDisposable
{
    private const char AcceptHeaderSeparator = ',';

    private readonly TelemetryHarness _harness = new();

    [Fact]
    public async Task HandleException_HtmlRequest_RedirectsToErrorPage()
    {
        // Arrange
        var context = BuildContext(
            new InvalidOperationException("boom"),
            MediaTypeNames.Text.Html + AcceptHeaderSeparator + MediaTypeNames.Application.Xml,
            out var mockProblemDetails);

        // Act
        await context.HandleException();

        // Assert
        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal(PageRoutes.Error, context.Response.Headers.Location.ToString());
        mockProblemDetails.Verify(p => p.WriteAsync(It.IsAny<ProblemDetailsContext>()), Times.Never);
    }

    [Fact]
    public async Task HandleException_JsonRequest_WritesProblemDetails500()
    {
        // Arrange
        var context = BuildContext(
            new InvalidOperationException("boom"),
            MediaTypeNames.Application.Json,
            out var mockProblemDetails);

        // Act
        await context.HandleException();

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        mockProblemDetails.Verify(
            p => p.WriteAsync(It.Is<ProblemDetailsContext>(c => c.ProblemDetails.Status == StatusCodes.Status500InternalServerError)),
            Times.Once);
    }

    [Fact]
    public async Task HandleException_WithException_RecordsActivityEvent()
    {
        // Arrange
        var ex = new InvalidOperationException("test-error");
        var context = BuildContext(
            ex,
            MediaTypeNames.Application.Json,
            out _);

        using var source = new ActivitySource(Generated.NewActivitySourceName());
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        ActivitySource.AddActivityListener(listener);

        using var activity = source.StartActivity(Generated.NewActivityName());

        // Act
        await context.HandleException();

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Contains(
            activity.Events,
            e => string.Equals(e.Name, Telemetry.Metrics.ExceptionEventName, StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandleException_WithException_IncrementsExceptionMetric()
    {
        // Arrange
        var ex = new InvalidOperationException("test-error");
        var context = BuildContext(
            ex,
            MediaTypeNames.Application.Json,
            out _);

        using var exceptionCounter = ExceptionCounterCapture.Start(_harness.MeterFactory);

        // Act
        await context.HandleException();

        // Assert
        Assert.Equal(1, exceptionCounter.Total);
        Assert.Equal(nameof(InvalidOperationException), exceptionCounter.ExceptionType);
    }

    public void Dispose() => _harness.Dispose();

    private DefaultHttpContext BuildContext(
        Exception? exception,
        string acceptHeader,
        out Mock<IProblemDetailsService> problemDetailsOut)
    {
        var mockProblemDetails = new Mock<IProblemDetailsService>(MockBehavior.Strict);
        mockProblemDetails
            .Setup(p => p.WriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Returns(ValueTask.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(mockProblemDetails.Object);
        services.AddSingleton(_harness.Telemetry);

        var context = new DefaultHttpContext();
        context.RequestServices = services.BuildServiceProvider();
        context.Request.Headers.Accept = acceptHeader;

        if (exception is not null)
        {
            var feature = Mock.Of<IExceptionHandlerFeature>(f => f.Error == exception);
            context.Features.Set(feature);
        }

        problemDetailsOut = mockProblemDetails;
        return context;
    }

    private sealed class ExceptionCounterCapture : IDisposable
    {
        private readonly MeterListener _listener = new();

        private ExceptionCounterCapture(IMeterFactory meterFactory)
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(instrument.Meter.Scope, meterFactory) &&
                    string.Equals(instrument.Name, Telemetry.Metrics.ExceptionCounterName, StringComparison.Ordinal))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((_, measurement, tags, _) =>
            {
                Total += measurement;
                foreach (var tag in tags)
                {
                    if (string.Equals(tag.Key, Telemetry.Metrics.ExceptionTypeTagName, StringComparison.Ordinal))
                    {
                        ExceptionType = tag.Value?.ToString();
                    }
                }
            });
        }

        public long Total { get; private set; }

        public string? ExceptionType { get; private set; }

        public static ExceptionCounterCapture Start(IMeterFactory meterFactory)
        {
            var capture = new ExceptionCounterCapture(meterFactory);
            capture._listener.Start();
            return capture;
        }

        public void Dispose() => _listener.Dispose();
    }
}
