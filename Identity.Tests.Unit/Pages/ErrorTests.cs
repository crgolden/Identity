namespace Identity.Tests.Unit.Pages;

using System.Diagnostics;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Identity.Pages;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class ErrorTests : IDisposable
{
    private const char NullCharacter = '\0';
    private const char LineFeed = '\n';
    private const char CarriageReturn = '\r';

    private readonly TelemetryHarness _harness = new();

    public static TheoryData<string?> RequestIdsWithNothingToShow() => new()
    {
        (string?)null,
        new string(LineFeed, 1),
        new string([CarriageReturn, LineFeed]),
    };

    [Fact]
    public void Constructor_ValidInteractionService_InitializesDefaults()
    {
        // Arrange
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);

        // Act
        var model = new Error(mockInteraction.Object, _harness.Telemetry);

        // Assert
        Assert.Null(model.RequestId);
        Assert.False(model.ShowRequestId);
    }

    [Fact]
    public async Task OnGetAsync_NullErrorIdWithCurrentActivity_UsesActivityIdAndSkipsInteractionService()
    {
        // Arrange
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var (model, _) = BuildModel(mockInteraction);
        using var activityScope = CurrentActivityScope.Start();

        // Act
        await model.OnGetAsync(null);

        // Assert
        VerifyInteractionServiceNeverCalled(mockInteraction);
        Assert.Equal(activityScope.ActivityId, model.RequestId);
    }

    [Fact]
    public async Task OnGetAsync_NullErrorIdWithNoCurrentActivity_UsesTraceIdentifierAndSkipsInteractionService()
    {
        // Arrange
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var (model, traceIdentifier) = BuildModel(mockInteraction);
        Activity.Current = null;

        // Act
        await model.OnGetAsync(null);

        // Assert
        VerifyInteractionServiceNeverCalled(mockInteraction);
        Assert.Equal(traceIdentifier, model.RequestId);
    }

    [Fact]
    public async Task OnGetAsync_WhitespaceErrorIdWithCurrentActivity_UsesActivityIdAndSkipsInteractionService()
    {
        // Arrange
        var whitespaceErrorId = Generated.NewWhitespaceValue();
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var (model, _) = BuildModel(mockInteraction);
        using var activityScope = CurrentActivityScope.Start();

        // Act
        await model.OnGetAsync(whitespaceErrorId);

        // Assert
        VerifyInteractionServiceNeverCalled(mockInteraction);
        Assert.Equal(activityScope.ActivityId, model.RequestId);
    }

    [Fact]
    public async Task OnGetAsync_WhitespaceErrorIdWithNoCurrentActivity_UsesTraceIdentifierAndSkipsInteractionService()
    {
        // Arrange
        var whitespaceErrorId = Generated.NewWhitespaceValue();
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var (model, traceIdentifier) = BuildModel(mockInteraction);
        Activity.Current = null;

        // Act
        await model.OnGetAsync(whitespaceErrorId);

        // Assert
        VerifyInteractionServiceNeverCalled(mockInteraction);
        Assert.Equal(traceIdentifier, model.RequestId);
    }

    [Fact]
    public async Task OnGetAsync_ErrorIdWithCurrentActivity_CallsInteractionServiceAndUsesActivityId()
    {
        // Arrange
        var errorId = $"error-{Guid.NewGuid():N}";
        var mockInteraction = BuildInteractionServiceReturningNoErrorContext(errorId);
        var (model, _) = BuildModel(mockInteraction);
        using var activityScope = CurrentActivityScope.Start();

        // Act
        var ex = await Record.ExceptionAsync(() => model.OnGetAsync(errorId));

        // Assert
        Assert.Null(ex);
        mockInteraction.Verify(s => s.GetErrorContextAsync(errorId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(activityScope.ActivityId, model.RequestId);
        Assert.True(model.ShowRequestId);
    }

    [Fact]
    public async Task OnGetAsync_ErrorIdWithNoCurrentActivity_CallsInteractionServiceAndUsesTraceIdentifier()
    {
        // Arrange
        var errorId = $"error-{Guid.NewGuid():N}";
        var mockInteraction = BuildInteractionServiceReturningNoErrorContext(errorId);
        var (model, traceIdentifier) = BuildModel(mockInteraction);
        Activity.Current = null;

        // Act
        var ex = await Record.ExceptionAsync(() => model.OnGetAsync(errorId));

        // Assert
        Assert.Null(ex);
        mockInteraction.Verify(s => s.GetErrorContextAsync(errorId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(traceIdentifier, model.RequestId);
        Assert.True(model.ShowRequestId);
    }

    [Fact]
    public async Task OnGetAsync_OverlongErrorId_CallsInteractionServiceAndUsesActivityId()
    {
        // Arrange
        var overlongErrorId = Generated.NewOverlongValue();
        var mockInteraction = BuildInteractionServiceReturningNoErrorContext(overlongErrorId);
        var (model, _) = BuildModel(mockInteraction);
        using var activityScope = CurrentActivityScope.Start();

        // Act
        var ex = await Record.ExceptionAsync(() => model.OnGetAsync(overlongErrorId));

        // Assert
        Assert.Null(ex);
        mockInteraction.Verify(s => s.GetErrorContextAsync(overlongErrorId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(activityScope.ActivityId, model.RequestId);
        Assert.True(model.ShowRequestId);
    }

    [Fact]
    public async Task OnGetAsync_ControlAndSymbolErrorId_CallsInteractionServiceAndUsesActivityId()
    {
        // Arrange
        var controlAndSymbolErrorId = Generated.NewControlAndSymbolValue();
        var mockInteraction = BuildInteractionServiceReturningNoErrorContext(controlAndSymbolErrorId);
        var (model, _) = BuildModel(mockInteraction);
        using var activityScope = CurrentActivityScope.Start();

        // Act
        var ex = await Record.ExceptionAsync(() => model.OnGetAsync(controlAndSymbolErrorId));

        // Assert
        Assert.Null(ex);
        mockInteraction.Verify(s => s.GetErrorContextAsync(controlAndSymbolErrorId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(activityScope.ActivityId, model.RequestId);
        Assert.True(model.ShowRequestId);
    }

    [Theory]
    [MemberData(nameof(RequestIdsWithNothingToShow))]
    public void ShowRequestId_NullOrLineBreaksOnly_IsFalse(string? requestId)
    {
        // Arrange
        var model = BuildModelWithRequestId(requestId);

        // Act
        var shown = model.ShowRequestId;

        // Assert
        Assert.False(shown);
    }

    [Fact]
    public void ShowRequestId_SpacesOnly_IsFalse()
    {
        // Arrange
        var spacesOnlyRequestId = Generated.NewWhitespaceValue();
        var model = BuildModelWithRequestId(spacesOnlyRequestId);

        // Act
        var shown = model.ShowRequestId;

        // Assert
        Assert.False(shown);
    }

    [Fact]
    public void ShowRequestId_NullCharacterOnly_IsTrue()
    {
        // Arrange
        var model = BuildModelWithRequestId(new string(NullCharacter, 1));

        // Act
        var shown = model.ShowRequestId;

        // Assert
        Assert.True(shown);
    }

    [Fact]
    public void ShowRequestId_RequestId_IsTrue()
    {
        // Arrange
        var requestId = Generated.NewRequestId();
        var model = BuildModelWithRequestId(requestId);

        // Act
        var shown = model.ShowRequestId;

        // Assert
        Assert.True(shown);
    }

    [Fact]
    public void ShowRequestId_OverlongValue_IsTrue()
    {
        // Arrange
        var overlongRequestId = Generated.NewOverlongValue();
        var model = BuildModelWithRequestId(overlongRequestId);

        // Act
        var shown = model.ShowRequestId;

        // Assert
        Assert.True(shown);
    }

    [Fact]
    public void ShowRequestId_PunctuatedValue_IsTrue()
    {
        // Arrange
        var punctuatedRequestId = Generated.NewPunctuatedPageName();
        var model = BuildModelWithRequestId(punctuatedRequestId);

        // Act
        var shown = model.ShowRequestId;

        // Assert
        Assert.True(shown);
    }

    [Fact]
    public async Task OnGetAsync_ValidErrorId_StartsOidcActivity()
    {
        // Arrange
        var oidcErrorId = $"error-{Guid.NewGuid():N}";
        var oidcError = $"access_denied-{Guid.NewGuid():N}";
        var errorMessage = new ErrorMessage { Error = oidcError, ErrorDescription = $"denied-{Guid.NewGuid():N}" };
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction
            .Setup(s => s.GetErrorContextAsync(oidcErrorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorMessage);

        var model = new Error(mockInteraction.Object, _harness.Telemetry)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };
        Activity.Current = null;

        using var oidcErrorActivity = OidcErrorActivityCapture.Start();

        // Act
        await model.OnGetAsync(oidcErrorId);

        // Assert
        Assert.NotNull(oidcErrorActivity.Captured);
        Assert.Equal(oidcError, oidcErrorActivity.Captured.GetTagItem(Error.OidcErrorTagName));
        Assert.Equal(oidcErrorId, oidcErrorActivity.Captured.GetTagItem(Error.OidcErrorIdTagName));
    }

    public void Dispose() => _harness.Dispose();

    private static void VerifyInteractionServiceNeverCalled(Mock<IIdentityServerInteractionService> mockInteraction) =>
        mockInteraction.Verify(
            s => s.GetErrorContextAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);

    private static Mock<IIdentityServerInteractionService> BuildInteractionServiceReturningNoErrorContext(string errorId)
    {
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction
            .Setup(s => s.GetErrorContextAsync(errorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErrorMessage?)null)
            .Verifiable();
        return mockInteraction;
    }

    private Error BuildModelWithRequestId(string? requestId) =>
        new(new Mock<IIdentityServerInteractionService>(MockBehavior.Strict).Object, _harness.Telemetry)
        {
            RequestId = requestId,
        };

    private (Error Model, string TraceIdentifier) BuildModel(
        Mock<IIdentityServerInteractionService> mockInteraction)
    {
        var traceIdentifier = $"trace-{Guid.NewGuid():N}";
        var httpContext = new DefaultHttpContext { TraceIdentifier = traceIdentifier };
        var model = new Error(mockInteraction.Object, _harness.Telemetry)
        {
            PageContext = new PageContext { HttpContext = httpContext }
        };
        return (model, traceIdentifier);
    }

    private sealed class CurrentActivityScope : IDisposable
    {
        private readonly Activity _activity;

        private CurrentActivityScope(Activity activity) => _activity = activity;

        public string ActivityId =>
            _activity.Id ?? throw new InvalidOperationException("Activity.Id should be set after Start.");

        public static CurrentActivityScope Start()
        {
            Activity.Current = null;
            var activity = new Activity($"unit-test-activity-{Guid.NewGuid():N}");
            activity.Start();
            return new CurrentActivityScope(activity);
        }

        public void Dispose()
        {
            _activity.Dispose();
            Activity.Current = null;
        }
    }

    private sealed class OidcErrorActivityCapture : IDisposable
    {
        private readonly ActivityListener _listener;

        private OidcErrorActivityCapture() =>
            _listener = new ActivityListener
            {
                ShouldListenTo = source => string.Equals(source.Name, Telemetry.SourceName, StringComparison.Ordinal),
                Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = started =>
                {
                    if (string.Equals(started.OperationName, Error.OidcErrorActivityName, StringComparison.Ordinal))
                    {
                        Captured = started;
                    }
                },
            };

        public Activity? Captured { get; private set; }

        public static OidcErrorActivityCapture Start()
        {
            var capture = new OidcErrorActivityCapture();
            ActivitySource.AddActivityListener(capture._listener);
            return capture;
        }

        public void Dispose() => _listener.Dispose();
    }
}
