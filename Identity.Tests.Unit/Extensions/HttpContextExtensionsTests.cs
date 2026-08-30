namespace Identity.Tests.Unit.Extensions;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Identity.Extensions;
using Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class HttpContextExtensionsTests
{
    [Fact]
    public async Task HandleException_HtmlRequest_RedirectsToErrorPage()
    {
        // Arrange
        var context = BuildContext(
            new InvalidOperationException("boom"),
            "text/html,application/xhtml+xml",
            out var mockProblemDetails);

        // Act
        await context.HandleException();

        // Assert
        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal("/Error", context.Response.Headers.Location.ToString());
        mockProblemDetails.Verify(p => p.WriteAsync(It.IsAny<ProblemDetailsContext>()), Times.Never);
    }

    [Fact]
    public async Task HandleException_JsonRequest_WritesProblemDetails500()
    {
        // Arrange
        var context = BuildContext(
            new InvalidOperationException("boom"),
            "application/json",
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
            "application/json",
            out _);

        using var source = new ActivitySource("test.source");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = source.StartActivity("test-operation");

        // Act
        await context.HandleException();

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Contains(activity.Events, e => string.Equals(e.Name, "exception", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandleException_WithException_IncrementsExceptionMetric()
    {
        // Arrange
        var ex = new InvalidOperationException("test-error");
        var context = BuildContext(
            ex,
            "application/json",
            out _);

        using var exceptionCounter = ExceptionCounterCapture.Start();

        // Act
        await context.HandleException();

        // Assert
        Assert.Equal(1, exceptionCounter.Total);
        Assert.Equal(nameof(InvalidOperationException), exceptionCounter.ExceptionType);
    }

    private static DefaultHttpContext BuildContext(
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

        private ExceptionCounterCapture()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (string.Equals(instrument.Meter.Name, nameof(Identity), StringComparison.Ordinal) &&
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

        public static ExceptionCounterCapture Start()
        {
            var capture = new ExceptionCounterCapture();
            capture._listener.Start();
            return capture;
        }

        public void Dispose() => _listener.Dispose();
    }
}