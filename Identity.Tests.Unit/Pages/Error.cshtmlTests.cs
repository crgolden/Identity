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
    public static TheoryData<MockBehavior> GetMockBehaviors() => new()
    {
        MockBehavior.Loose,
        MockBehavior.Strict,
    };

    public static TheoryData<string?, bool> NoErrorIdCases() => new()
    {
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
        var specialId = "err\0or\n\t☃-!@#$%^&*()";
        return new TheoryData<string, bool>
        {
            { "error-123", true },
            { "error-123", false },
            { longId, true },
            { longId, false },
            { specialId, true },
            { specialId, false },
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

    [Theory]
    [MemberData(nameof(GetMockBehaviors))]
    public void Constructor_ValidInteractionService_InitializesDefaults(MockBehavior mockBehavior)
    {
        // Arrange
        var mockInteraction = new Mock<IIdentityServerInteractionService>(mockBehavior);

        // Act
        var exception = Record.Exception(() => new ErrorModel(mockInteraction.Object));
        var model = exception is null ? new ErrorModel(mockInteraction.Object) : null;

        // Assert
        Assert.Null(exception);
        Assert.NotNull(model);
        Assert.Null(model?.RequestId);
        Assert.False(model?.ShowRequestId);
    }

    [Theory]
    [MemberData(nameof(NoErrorIdCases))]
    public async Task OnGetAsync_NullOrWhitespaceErrorId_SkipsInteractionService(string? errorId, bool setActivity)
    {
        // Arrange
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        var model = new ErrorModel(mockInteraction.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = $"trace-{Guid.NewGuid():N}";
        model.PageContext = new PageContext { HttpContext = httpContext };

        Activity? activity = null;
        string expectedRequestId;
        if (setActivity)
        {
            Activity.Current = null;
            activity = new Activity("unit-test-activity");
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
            await model.OnGetAsync(errorId);

            // Assert
            mockInteraction.Verify(
                s => s.GetErrorContextAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "GetErrorContextAsync should not be called when errorId is null/empty/whitespace.");

            Assert.Equal(expectedRequestId, model.RequestId);
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

    [Theory]
    [MemberData(nameof(NonEmptyErrorIdCases))]
    public async Task OnGetAsync_ValidErrorId_CallsInteractionService(string errorId, bool setActivity)
    {
        // Arrange
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction
            .Setup(s => s.GetErrorContextAsync(It.Is<string>(id => id == errorId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErrorMessage?)null)
            .Verifiable();

        var model = new ErrorModel(mockInteraction.Object);

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

            // Assert
            Assert.Null(ex);

            mockInteraction.Verify(s => s.GetErrorContextAsync(errorId, It.IsAny<CancellationToken>()), Times.Once);

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
        var errorMessage = new ErrorMessage { Error = "access_denied", ErrorDescription = "User denied access." };
        var mockInteraction = new Mock<IIdentityServerInteractionService>(MockBehavior.Strict);
        mockInteraction
            .Setup(s => s.GetErrorContextAsync("error-abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorMessage);

        var model = new ErrorModel(mockInteraction.Object);
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        Activity.Current = null;

        Activity? capturedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => string.Equals(source.Name, nameof(Identity), StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity =>
            {
                if (string.Equals(activity.OperationName, "identity.error.oidc", StringComparison.Ordinal))
                {
                    capturedActivity = activity;
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        await model.OnGetAsync("error-abc");

        // Assert
        Assert.NotNull(capturedActivity);
        Assert.Equal("access_denied", capturedActivity.GetTagItem("oidc.error"));
        Assert.Equal("error-abc", capturedActivity.GetTagItem("oidc.error_id"));
    }
}