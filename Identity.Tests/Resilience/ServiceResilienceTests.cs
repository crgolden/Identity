namespace Identity.Tests.Resilience;

using System.Net;
using Moq;
using Resend;

/// <summary>
/// Resilience tests verifying that external service failures propagate correctly
/// and do not corrupt application state or swallow exceptions unexpectedly.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ServiceResilienceTests
{
    // EmailSender resilience
    [Fact]
    public async Task EmailSender_WhenResendThrowsApiException_PropagatesException()
    {
        var mockResend = new Mock<IResend>();
        mockResend
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ResendException(HttpStatusCode.TooManyRequests, ErrorType.RateLimitExceeded, "Rate limited", null, null));

        EmailSender sender = new EmailSender(mockResend.Object);

        await Assert.ThrowsAsync<ResendException>(
            () => sender.SendEmailAsync("to@example.com", "Subject", "Body"));
    }

    [Fact]
    public async Task EmailSender_WhenResendThrowsHttpRequestException_PropagatesException()
    {
        var mockResend = new Mock<IResend>();
        mockResend
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        EmailSender sender = new EmailSender(mockResend.Object);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => sender.SendEmailAsync("to@example.com", "Subject", "Body"));
    }

    [Fact]
    public async Task EmailSender_WhenResendThrowsOperationCanceled_PropagatesException()
    {
        var mockResend = new Mock<IResend>();
        mockResend
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        EmailSender sender = new EmailSender(mockResend.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sender.SendEmailAsync("to@example.com", "Subject", "Body"));
    }
}
