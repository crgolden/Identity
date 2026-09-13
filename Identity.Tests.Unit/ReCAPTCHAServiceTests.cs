namespace Identity.Tests.Unit;

using System.Net;
using System.Net.Mime;
using System.Text.Json.Nodes;
using CAPTCHA;
using Infrastructure;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ReCAPTCHAServiceTests
{
    private const string SendAsyncMethodName = nameof(HttpClient.SendAsync);
    private const string SiteverifySuccessFieldName = ReCAPTCHAService.SiteverifySuccessFieldName;
    private const string SiteverifyScoreFieldName = ReCAPTCHAService.SiteverifyScoreFieldName;

    [Fact]
    public async Task VerifyAsync_NullToken_FailsWithZeroScore()
    {
        var (service, _) = CreateService(responseScore: TestValues.NewScoreAtOrAboveDefaultThreshold());
        var verdict = await service.VerifyAsync(null, TestContext.Current.CancellationToken);
        Assert.False(verdict.Passed);
        Assert.Equal(decimal.Zero, verdict.Score);
    }

    [Fact]
    public async Task VerifyAsync_EmptyToken_FailsWithZeroScore()
    {
        var (service, _) = CreateService(responseScore: TestValues.NewScoreAtOrAboveDefaultThreshold());
        var verdict = await service.VerifyAsync(string.Empty, TestContext.Current.CancellationToken);
        Assert.False(verdict.Passed);
        Assert.Equal(decimal.Zero, verdict.Score);
    }

    [Fact]
    public async Task VerifyAsync_NullSecretKey_FailsWithZeroScore()
    {
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, _) = CreateService(responseScore: TestValues.NewScoreAtOrAboveDefaultThreshold(), secretKeyConfigured: false);
        var verdict = await service.VerifyAsync(submittedRecaptchaToken, TestContext.Current.CancellationToken);
        Assert.False(verdict.Passed);
        Assert.Equal(decimal.Zero, verdict.Score);
    }

    [Fact]
    public async Task VerifyAsync_ScoreAtOrAboveThreshold_Passes()
    {
        var scoreAtOrAboveThreshold = TestValues.NewScoreAtOrAboveDefaultThreshold();
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, _) = CreateService(responseScore: scoreAtOrAboveThreshold);
        var verdict = await service.VerifyAsync(submittedRecaptchaToken, TestContext.Current.CancellationToken);
        Assert.True(verdict.Passed);
        Assert.Equal(scoreAtOrAboveThreshold, verdict.Score);
    }

    [Fact]
    public async Task VerifyAsync_ScoreBelowThreshold_FailsClosed()
    {
        var scoreBelowThreshold = TestValues.NewScoreBelowDefaultThreshold();
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, _) = CreateService(responseScore: scoreBelowThreshold);
        var verdict = await service.VerifyAsync(submittedRecaptchaToken, TestContext.Current.CancellationToken);
        Assert.False(verdict.Passed);
        Assert.Equal(scoreBelowThreshold, verdict.Score);
    }

    [Fact]
    public async Task VerifyAsync_ApiReturnsFalseSuccess_Fails()
    {
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, _) = CreateService(responseScore: TestValues.NewScoreAtOrAboveDefaultThreshold(), success: false);
        var verdict = await service.VerifyAsync(submittedRecaptchaToken, TestContext.Current.CancellationToken);
        Assert.False(verdict.Passed);
        Assert.Equal(decimal.Zero, verdict.Score);
    }

    [Fact]
    public async Task VerifyAsync_HttpFailure_Fails()
    {
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, _) = CreateService(responseScore: TestValues.NewScoreAtOrAboveDefaultThreshold(), httpStatusCode: HttpStatusCode.ServiceUnavailable);
        var verdict = await service.VerifyAsync(submittedRecaptchaToken, TestContext.Current.CancellationToken);
        Assert.False(verdict.Passed);
        Assert.Equal(decimal.Zero, verdict.Score);
    }

    [Fact]
    public async Task VerifyAsync_CallsSiteverifyExactlyOnce()
    {
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, handlerMock) = CreateService(responseScore: TestValues.NewScoreAtOrAboveDefaultThreshold());
        await service.VerifyAsync(submittedRecaptchaToken, TestContext.Current.CancellationToken);
        handlerMock.Protected().Verify(
            SendAsyncMethodName,
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    private static (ReCAPTCHAService Service, Mock<HttpMessageHandler> HandlerMock) CreateService(
        decimal responseScore,
        bool success = true,
        HttpStatusCode httpStatusCode = HttpStatusCode.OK,
        bool secretKeyConfigured = true)
    {
        var secretKey = secretKeyConfigured ? TestValues.NewRecaptchaSecretKey() : null;
        var json = new JsonObject
        {
            [SiteverifySuccessFieldName] = success,
            [SiteverifyScoreFieldName] = responseScore,
        }.ToJsonString();

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                SendAsyncMethodName,
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = httpStatusCode,
                Content = new StringContent(json, System.Text.Encoding.UTF8, MediaTypeNames.Application.Json)
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new UriBuilder(Uri.UriSchemeHttps, TestValues.NewExternalHost()).Uri
        };
        var optionsMock = new Mock<IOptions<ReCAPTCHAOptions>>(MockBehavior.Strict);
        optionsMock.Setup(o => o.Value).Returns(new ReCAPTCHAOptions
        {
            SecretKey = secretKey
        });
        return (new ReCAPTCHAService(httpClient, optionsMock.Object), handlerMock);
    }
}
