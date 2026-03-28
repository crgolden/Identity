namespace Identity.Tests.Pages;

using System.Diagnostics;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Identity.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;

/// <summary>Unit tests for <see cref="Identity.Pages.ErrorModel"/>.</summary>
[Trait("Category", "Unit")]
public class ErrorModelTests
{
    /// <summary>
    /// Provides MockBehavior values to exercise constructor under different mock configurations.
    /// </summary>
    /// <returns></returns>
    public static TheoryData<MockBehavior> GetMockBehaviors() => new()
    {
        MockBehavior.Loose,
        MockBehavior.Strict,
    };

    public static TheoryData<string?, bool> NoErrorIdCases() => new()
    {
        // Each combination: (errorId, setActivity)
        { null, true },
        { null, false },
        { string.Empty, true },
        { string.Empty, false },
        { "   ", true },
        { "   ", false },
    };

    public static TheoryData<string, bool> NonEmptyErrorIdCases()
    {
        var longId = new string('x', 5000);
        var specialId = "err\0or\n\t\u2603-!@#$%^&*()";
        return new TheoryData<string, bool>
        {
            // Normal id
            { "error-123", true },
            { "error-123", false },

            // Very long id (boundary)
            { longId, true },
            { longId, false },

            // Special and control characters
            { specialId, true },
            { specialId, false },
        };
    }

    public static TheoryData<string?, bool> RequestIdTestCases() => new()
    {
        // null => IsNullOrWhiteSpace(null) == true => ShowRequestId == false
        { null, false },

        // empty string => IsNullOrWhiteSpace("") == true => ShowRequestId == false
        { string.Empty, false },

        // whitespace-only string => IsNullOrWhiteSpace(" ") == true => ShowRequestId == false
        { " ", false },

        // typical non-empty string => true
        { "request-123", true },

        // very long string => true
        { new string('x', 10000), true },

        // null character inside string => not empty => true
        { "\0", true },

        // newline characters => IsNullOrWhiteSpace => false
        { "\n", false },
        { "\r\n", false },

        // string with special characters => true
        { "\u00A9\u00AE\u2122!@#$%^&*()", true },
    };

    /// <summary>
    /// Verifies that the constructor accepts a valid IIdentityServerInteractionService and
    /// produces a usable ErrorModel instance without throwing. This uses different mock
    /// behaviors to ensure the constructor is resilient to the mock's configuration.
    /// Input conditions: a non-null mocked IIdentityServerInteractionService with the provided MockBehavior.
    /// Expected result: ErrorModel instance is created successfully, RequestId is null, and ShowRequestId is false.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetMockBehaviors))]
    public void Constructor_ValidInteractionService_InitializesDefaults(MockBehavior mockBehavior)
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ErrorModel>>();
        var mockInteraction = new Mock<IIdentityServerInteractionService>(mockBehavior);

        // Act
        var exception = Record.Exception(() => new ErrorModel(mockLogger.Object, mockInteraction.Object));
        var model = exception is null ? new ErrorModel(mockLogger.Object, mockInteraction.Object) : null;

        // Assert
        Assert.Null(exception);
        Assert.NotNull(model);

        // Initial RequestId should be null (not set by constructor)
        Assert.Null(model?.RequestId);

        // ShowRequestId should be false when RequestId is null or empty
        Assert.False(model?.ShowRequestId);
    }

    /// <summary>
    /// Tests that when errorId is null, empty, or whitespace the interaction service is NOT called,
    /// and the RequestId is taken from Activity.Current.Id when present, otherwise from HttpContext.TraceIdentifier.
    /// Inputs tested: null, empty string, whitespace-only string; with Activity.Current present and absent.
    /// Expected: no call to GetErrorContextAsync and RequestId equals the activity id (if set) or trace identifier.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Theory]
    [MemberData(nameof(NoErrorIdCases))]
    public async Task OnGetAsync_NullOrWhitespaceErrorId_SkipsInteractionService(string? errorId, bool setActivity)
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ErrorModel>>();
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);

        // No setup for GetErrorContextAsync - we expect it to not be called.
        var model = new ErrorModel(mockLogger.Object, mockInteraction.Object);

        // Prepare HttpContext with a known trace identifier.
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = $"trace-{Guid.NewGuid():N}";
        model.PageContext = new PageContext { HttpContext = httpContext };

        Activity? activity = null;
        string expectedRequestId;
        if (setActivity)
        {
            // Ensure no prior current activity
            Activity.Current = null;
            activity = new Activity("unit-test-activity");
            activity.Start();
            expectedRequestId = activity.Id ?? throw new InvalidOperationException("Activity.Id should be set after Start");
        }
        else
        {
            // Ensure no current activity
            Activity.Current = null;
            expectedRequestId = httpContext.TraceIdentifier;
        }

        try
        {
            // Act
            await model.OnGetAsync(errorId);

            // Assert
            mockInteraction.Verify(
                s => s.GetErrorContextAsync(It.IsAny<string?>()),
                Times.Never,
                "GetErrorContextAsync should not be called when errorId is null/empty/whitespace.");

            Assert.Equal(expectedRequestId, model.RequestId);
        }
        finally
        {
            if (activity is not null)
            {
                activity.Stop();

                // Clear current to avoid affecting other tests.
                Activity.Current = null;
            }
        }
    }

    /// <summary>
    /// Tests that when a non-empty errorId is provided, the interaction service is called exactly once with that id,
    /// and RequestId is set from Activity.Current.Id if present or from HttpContext.TraceIdentifier otherwise.
    /// Inputs tested: normal id, very long id, id with special/control characters; with Activity.Current present and absent.
    /// Expected: GetErrorContextAsync invoked once with the exact id, no exception thrown even if service returns null,
    /// and RequestId set appropriately. Also ShowRequestId is true when RequestId is not null/empty.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Theory]
    [MemberData(nameof(NonEmptyErrorIdCases))]
    public async Task OnGetAsync_ValidErrorId_CallsInteractionService(string errorId, bool setActivity)
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ErrorModel>>();
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);

        // Return null from service to ensure code handles nullable returns without throwing.
        mockInteraction
            .Setup(s => s.GetErrorContextAsync(It.Is<string>(id => id == errorId)))
            .ReturnsAsync((ErrorMessage?)null)
            .Verifiable();

        var model = new ErrorModel(mockLogger.Object, mockInteraction.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = $"trace-{Guid.NewGuid():N}";
        model.PageContext = new PageContext { HttpContext = httpContext };

        Activity? activity = null;
        string expectedRequestId;
        if (setActivity)
        {
            Activity.Current = null;
            activity = new Activity("unit-test-activity-nonempty");
            activity.Start();
            expectedRequestId = activity.Id ?? throw new InvalidOperationException("Activity.Id should be set after Start");
        }
        else
        {
            Activity.Current = null;
            expectedRequestId = httpContext.TraceIdentifier;
        }

        try
        {
            // Act
            var ex = await Record.ExceptionAsync(() => model.OnGetAsync(errorId));

            // Assert - no exception thrown
            Assert.Null(ex);

            mockInteraction.Verify(s => s.GetErrorContextAsync(errorId), Times.Once);

            Assert.Equal(expectedRequestId, model.RequestId);
            Assert.True(model.ShowRequestId, "ShowRequestId should be true when RequestId is set.");
        }
        finally
        {
            if (activity is not null)
            {
                activity.Stop();
                Activity.Current = null;
            }
        }
    }

    /// <summary>
    /// Verifies ShowRequestId returns expected boolean depending on various RequestId inputs.
    /// Tests null, empty, whitespace-only, typical non-empty, very long, and control-character strings.
    /// Expected: false for null and empty; true for any non-empty value (including whitespace and control chars).
    /// </summary>
    [Theory]
    [MemberData(nameof(RequestIdTestCases))]
    public void ShowRequestId_VariousValues_ReturnsExpected(string? requestId, bool expected)
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ErrorModel>>();
        var mockInteraction = new Mock<IIdentityServerInteractionService>();
        var model = new ErrorModel(mockLogger.Object, mockInteraction.Object)
        {
            RequestId = requestId
        };

        // Act
        var actual = model.ShowRequestId;

        // Assert
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Verifies that when a non-empty errorId is provided and the interaction service returns an ErrorMessage,
    /// the logger's LogError method is invoked with that error message.
    /// Input: a valid errorId and a non-null ErrorMessage returned by the service.
    /// Expected: ILogger.Log called exactly once at LogLevel.Error.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task OnGetAsync_ValidErrorId_WithErrorMessage_LogsError()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ErrorModel>>();
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var errorMessage = new ErrorMessage { Error = "access_denied", ErrorDescription = "User denied access." };
        mockInteraction
            .Setup(s => s.GetErrorContextAsync("error-abc"))
            .ReturnsAsync(errorMessage);

        var model = new ErrorModel(mockLogger.Object, mockInteraction.Object);
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        Activity.Current = null;

        // Act
        await model.OnGetAsync("error-abc");

        // Assert
        mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "LogError should be called once when GetErrorContextAsync returns an error message.");
    }
}
