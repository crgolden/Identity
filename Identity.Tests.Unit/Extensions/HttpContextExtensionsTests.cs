namespace Identity.Tests.Unit.Extensions;
using Infrastructure;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Identity.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

/// <summary>Unit tests for <see cref="HttpContextExtensions"/>.</summary>
[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class HttpContextExtensionsTests
{
    /// <summary>
    /// Verifies that a browser request (Accept: text/html) is redirected to /Error and
    /// problem details are NOT written.
    /// </summary>
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

    /// <summary>
    /// Verifies that a non-HTML request receives a 500 problem details response.
    /// </summary>
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

    /// <summary>
    /// Verifies that when an Activity is current, its status is set to Error and an "exception" event is recorded.
    /// </summary>
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

    /// <summary>
    /// Verifies that the identity.exceptions counter is incremented by 1 with the correct exception.type tag.
    /// </summary>
    [Fact]
    public async Task HandleException_WithException_IncrementsExceptionMetric()
    {
        // Arrange
        var ex = new InvalidOperationException("test-error");
        var context = BuildContext(
            ex,
            "application/json",
            out _);

        long capturedValue = 0;
        string? capturedTag = null;

        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (string.Equals(instrument.Meter.Name, nameof(Identity), StringComparison.Ordinal) &&
                string.Equals(instrument.Name, "identity.exceptions", StringComparison.Ordinal))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            capturedValue += measurement;
            foreach (var tag in tags)
            {
                if (string.Equals(tag.Key, "exception.type", StringComparison.Ordinal))
                {
                    capturedTag = tag.Value?.ToString();
                }
            }
        });
        meterListener.Start();

        // Act
        await context.HandleException();

        // Assert
        Assert.Equal(1, capturedValue);
        Assert.Equal(nameof(InvalidOperationException), capturedTag);
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
}
