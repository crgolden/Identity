namespace Identity.Tests;

using Moq;
using Resend;

/// <summary>
/// Tests for Identity.EmailSender.
/// </summary>
[Trait("Category", "Unit")]
public class EmailSenderTests
{
    public static TheoryData<MockBehavior> MockBehaviors() => new()
    {
        MockBehavior.Default,
        MockBehavior.Strict,
    };

    /// <summary>
    /// Verifies that SendEmailAsync constructs an EmailMessage with the expected properties
    /// and calls IResend.EmailSendAsync. Tests multiple combinations of toEmail / subject / message
    /// including empty, whitespace and long strings to exercise boundary and special-character cases.
    /// Expected: IResend.EmailSendAsync is invoked and the EmailMessage contains the provided values.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Theory]
    [InlineData("user@example.com", "Hello", "Body text")]
    [InlineData("user+tag@example.com", "", "")] // empty subject and message
    [InlineData("user@example.com", "   ", "   ")] // whitespace-only subject and message
    [InlineData("special@chars.example", "\u00BFHola! \u00A1", "<p>HTML & <b>bold</b></p>")] // special chars / HTML
    [InlineData("long@example.com", "L" /* placeholder */, "M" /* placeholder */)]
    public async Task SendEmailAsync_VariousInputs_CallsResendWithExpectedMessage(string toEmail, string subject, string message)
    {
        // Arrange
        // Provide long strings for the last InlineData case
        if (subject == "L")
        {
            subject = new string('S', 2000);
        }

        if (message == "M")
        {
            message = new string('B', 5000);
        }

        var mockResend = new Mock<IResend>(MockBehavior.Strict);

        EmailMessage? capturedMessage = null;

        mockResend
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((m, ct) => capturedMessage = m)

            // Return a completed generic Task; the inner value is not used by EmailSender.
            .Returns(Task.FromResult<ResendResponse<Guid>>(null!));

        var sender = new EmailSender(mockResend.Object);

        // Act
        var task = sender.SendEmailAsync(toEmail, subject, message);
        await task; // should complete without throwing

        // Assert
        mockResend.Verify(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(capturedMessage);
        Assert.Equal(subject, capturedMessage!.Subject);
        Assert.Equal(message, capturedMessage.TextBody);
        Assert.Equal(message, capturedMessage.HtmlBody);

        // From is assigned in implementation; ensure it is present.
        Assert.NotNull(capturedMessage.From);

        // The recipient list should have exactly one entry added by SendEmailAsync.
        Assert.NotNull(capturedMessage.To);
        Assert.Single(capturedMessage.To);
    }

    /// <summary>
    /// Verifies that exceptions thrown by the IResend.EmailSendAsync are propagated by SendEmailAsync.
    /// Input: valid toEmail, subject and message. Expected: the same exception type is thrown to caller.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task SendEmailAsync_ResendThrows_PropagatesException()
    {
        // Arrange
        var toEmail = "user@example.com";
        var subject = "Subject";
        var message = "Body";

        var mockResend = new Mock<IResend>(MockBehavior.Strict);

        mockResend
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Resend failure"));

        var sender = new EmailSender(mockResend.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendEmailAsync(toEmail, subject, message));
        Assert.Equal("Resend failure", ex.Message);

        mockResend.Verify(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that the EmailSender constructor accepts a valid IResend dependency and
    /// creates a non-null instance. Tests different mock behaviors to ensure constructor
    /// does not rely on specific mock configuration.
    /// Input conditions: a non-null Mock{IResend} with specified MockBehavior.
    /// Expected result: no exception and a non-null EmailSender instance of the correct type.
    /// </summary>
    [Theory]
    [MemberData(nameof(MockBehaviors))]
    public void Constructor_ValidResend_CreatesInstance(MockBehavior behavior)
    {
        // Arrange
        var mockResend = new Mock<IResend>(behavior);

        // Act
        var sender = Record.Exception(() => new EmailSender(mockResend.Object)) is null
            ? new EmailSender(mockResend.Object)
            : throw new InvalidOperationException("Constructor threw an exception.");

        // Assert
        Assert.NotNull(sender);
        Assert.IsType<EmailSender>(sender);
    }

    /// <summary>
    /// Verifies that constructing EmailSender with different IResend instances produces
    /// distinct EmailSender objects (no unintended shared state at construction).
    /// Input conditions: two distinct Mock{IResend} instances.
    /// Expected result: two EmailSender instances are not the same reference.
    /// </summary>
    [Fact]
    public void Constructor_DifferentResendInstances_CreatesDistinctInstances()
    {
        // Arrange
        var mockA = new Mock<IResend>(MockBehavior.Default);
        var mockB = new Mock<IResend>(MockBehavior.Default);

        // Act
        var senderA = new EmailSender(mockA.Object);
        var senderB = new EmailSender(mockB.Object);

        // Assert
        Assert.NotSame(senderA, senderB);
        Assert.IsType<EmailSender>(senderA);
        Assert.IsType<EmailSender>(senderB);
    }
}
