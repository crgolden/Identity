namespace Identity.Tests.Unit.Pages;

using System.Diagnostics;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Identity.Pages;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ErrorModelTests
{
    public static TheoryData<string?> ErrorIdsThatSkipTheInteractionService() => new()
    {
        (string?)null,
        string.Empty,
        "   ",
    };

    public static TheoryData<string> ErrorIdsThatReachTheInteractionService()
    {
        var longId = new string('x', 5000);
        var specialId = "err\0or\n\t☃-!@#$%^&*()";
        return new TheoryData<string>
        {
            $"error-{Guid.NewGuid():N}",
            longId,
            specialId,
        };
    }

    public static TheoryData<string?, bool> RequestIdTestCases() => new()
    {
        { null, false },
        { string.Empty, false },
        { " ", false },
        { "request-123", true },
        { new string('x', 10000), true },
        { "\0", true },
        { "\n", false },
        { "\r\n", false },
        { "©®™!@#$%^&*()", true },
    };

    [Fact]
    public void Constructor_ValidInteractionService_InitializesDefaults()
    {
        // Arrange
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);

        // Act
        var model = new ErrorModel(mockInteraction.Object);

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
        var model = BuildModel(mockInteraction, out _);
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
        var model = BuildModel(mockInteraction, out var traceIdentifier);
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
        var model = BuildModel(mockInteraction, out _);
        using var activityScope = CurrentActivityScope.Start();

        // Act
        var ex = await Record.ExceptionAsync(() => model.OnGetAsync(errorId));

        // Assert
        Assert.Null(ex);
        mockInteraction.Verify(s => s.GetErrorContextAsync(errorId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(activityScope.ActivityId, model.RequestId);
        Assert.True(model.ShowRequestId, "ShowRequestId should be true when RequestId is set.");
    }

    [Theory]
    [MemberData(nameof(ErrorIdsThatReachTheInteractionService))]
    public async Task OnGetAsync_ErrorIdWithNoCurrentActivity_CallsInteractionServiceAndUsesTraceIdentifier(string errorId)
    {
        // Arrange
        var mockInteraction = BuildInteractionServiceReturningNoErrorContext(errorId);
        var model = BuildModel(mockInteraction, out var traceIdentifier);
        Activity.Current = null;

        // Act
        var ex = await Record.ExceptionAsync(() => model.OnGetAsync(errorId));

        // Assert
        Assert.Null(ex);
        mockInteraction.Verify(s => s.GetErrorContextAsync(errorId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(traceIdentifier, model.RequestId);
        Assert.True(model.ShowRequestId, "ShowRequestId should be true when RequestId is set.");
    }

    [Theory]
    [MemberData(nameof(RequestIdTestCases))]
    public void ShowRequestId_VariousValues_ReturnsExpected(string? requestId, bool expected)
    {
        // Arrange
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = new ErrorModel(mockInteraction.Object)
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

        var model = new ErrorModel(mockInteraction.Object)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };
        Activity.Current = null;

        using var oidcErrorActivity = OidcErrorActivityCapture.Start();

        // Act
        await model.OnGetAsync(oidcErrorId);

        // Assert
        Assert.NotNull(oidcErrorActivity.Captured);
        Assert.Equal(oidcError, oidcErrorActivity.Captured.GetTagItem(ErrorModel.OidcErrorTagName));
        Assert.Equal(oidcErrorId, oidcErrorActivity.Captured.GetTagItem(ErrorModel.OidcErrorIdTagName));
    }

    private static void VerifyInteractionServiceNeverCalled(Mock<IIdentityServerInteractionService> mockInteraction) =>
        mockInteraction.Verify(
            s => s.GetErrorContextAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "GetErrorContextAsync should not be called when errorId is null/empty/whitespace.");

    private static Mock<IIdentityServerInteractionService> BuildInteractionServiceReturningNoErrorContext(string errorId)
    {
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction
            .Setup(s => s.GetErrorContextAsync(errorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErrorMessage?)null)
            .Verifiable();
        return mockInteraction;
    }

    private static ErrorModel BuildModel(
        Mock<IIdentityServerInteractionService> mockInteraction,
        out string traceIdentifier)
    {
        traceIdentifier = $"trace-{Guid.NewGuid():N}";
        var httpContext = new DefaultHttpContext { TraceIdentifier = traceIdentifier };
        return new ErrorModel(mockInteraction.Object)
        {
            PageContext = new PageContext { HttpContext = httpContext }
        };
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
                ShouldListenTo = source => string.Equals(source.Name, nameof(Identity), StringComparison.Ordinal),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = started =>
                {
                    if (string.Equals(started.OperationName, ErrorModel.OidcErrorActivityName, StringComparison.Ordinal))
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