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

    public static TheoryData<string?> ErrorIdsThatSkipTheInteractionService() => new()
    {
        (string?)null,
        string.Empty,
        Generated.NewWhitespaceValue(),
    };

    public static TheoryData<string> ErrorIdsThatReachTheInteractionService()
    {
        var errorId = $"error-{Guid.NewGuid():N}";
        var longId = Generated.NewOverlongValue();
        var specialId = Generated.NewControlAndSymbolValue();
        return new TheoryData<string>
        {
            errorId,
            longId,
            specialId,
        };
    }

    public static TheoryData<string?, bool> RequestIdTestCases() => new()
    {
        { null, false },
        { string.Empty, false },
        { Generated.NewWhitespaceValue(), false },
        { Generated.NewRequestId(), true },
        { Generated.NewOverlongValue(), true },
        { new string(NullCharacter, 1), true },
        { new string(LineFeed, 1), false },
        { new string([CarriageReturn, LineFeed]), false },
        { Generated.NewPunctuatedPageName(), true },
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

    [Theory]
    [MemberData(nameof(ErrorIdsThatSkipTheInteractionService))]
    public async Task OnGetAsync_BlankErrorIdWithCurrentActivity_UsesActivityIdAndSkipsInteractionService(string? errorId)
    {
        // Arrange
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var (model, _) = BuildModel(mockInteraction);
        using var activityScope = CurrentActivityScope.Start();

        // Act
        await model.OnGetAsync(errorId);

        // Assert
        VerifyInteractionServiceNeverCalled(mockInteraction);
        Assert.Equal(activityScope.ActivityId, model.RequestId);
    }

    [Theory]
    [MemberData(nameof(ErrorIdsThatSkipTheInteractionService))]
    public async Task OnGetAsync_BlankErrorIdWithNoCurrentActivity_UsesTraceIdentifierAndSkipsInteractionService(string? errorId)
    {
        // Arrange
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var (model, traceIdentifier) = BuildModel(mockInteraction);
        Activity.Current = null;

        // Act
        await model.OnGetAsync(errorId);

        // Assert
        VerifyInteractionServiceNeverCalled(mockInteraction);
        Assert.Equal(traceIdentifier, model.RequestId);
    }

    [Theory]
    [MemberData(nameof(ErrorIdsThatReachTheInteractionService))]
    public async Task OnGetAsync_ErrorIdWithCurrentActivity_CallsInteractionServiceAndUsesActivityId(string errorId)
    {
        // Arrange
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

    [Theory]
    [MemberData(nameof(ErrorIdsThatReachTheInteractionService))]
    public async Task OnGetAsync_ErrorIdWithNoCurrentActivity_CallsInteractionServiceAndUsesTraceIdentifier(string errorId)
    {
        // Arrange
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

    [Theory]
    [MemberData(nameof(RequestIdTestCases))]
    public void ShowRequestId_VariousValues_ReturnsExpected(string? requestId, bool expected)
    {
        // Arrange
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = new Error(mockInteraction.Object, _harness.Telemetry)
        {
            RequestId = requestId
        };

        // Act
        var actual = model.ShowRequestId;

        // Assert
        Assert.Equal(expected, actual);
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
