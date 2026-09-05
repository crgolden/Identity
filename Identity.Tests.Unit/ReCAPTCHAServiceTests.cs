namespace Identity.Tests.Unit;

using System.Globalization;
using System.Net;
using CAPTCHA;
using Infrastructure;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ReCAPTCHAServiceTests
{
    [Fact]
    public async Task VerifyAsync_NullToken_FailsWithZeroScore()
    {
        var (service, _) = CreateService(responseScore: TestValues.NewScoreAtOrAboveDefaultThreshold());
        var verdict = await service.VerifyAsync(CAPTCHAActions.Login, TestValues.NewEmailAddress(), null, null, TestContext.Current.CancellationToken);
        Assert.False(verdict.Passed);
        Assert.Equal(0m, verdict.Score);
        Assert.False(verdict.MonitorOnly);
    }

    [Fact]
    public async Task VerifyAsync_EmptyToken_FailsWithZeroScore()
    {
        var (service, _) = CreateService(responseScore: TestValues.NewScoreAtOrAboveDefaultThreshold());
        var verdict = await service.VerifyAsync(CAPTCHAActions.Login, TestValues.NewEmailAddress(), string.Empty, null, TestContext.Current.CancellationToken);
        Assert.False(verdict.Passed);
        Assert.Equal(0m, verdict.Score);
    }

    [Fact]
    public async Task VerifyAsync_NullSecretKey_FailsWithZeroScore()
    {
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, _) = CreateService(responseScore: TestValues.NewScoreAtOrAboveDefaultThreshold(), secretKeyConfigured: false);
        var verdict = await service.VerifyAsync(CAPTCHAActions.Login, TestValues.NewEmailAddress(), submittedRecaptchaToken, null, TestContext.Current.CancellationToken);
        Assert.False(verdict.Passed);
        Assert.Equal(0m, verdict.Score);
    }

    [Fact]
    public async Task VerifyAsync_ScoreAtOrAboveThreshold_Passes()
    {
        var scoreAtOrAboveThreshold = TestValues.NewScoreAtOrAboveDefaultThreshold();
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, _) = CreateService(responseScore: scoreAtOrAboveThreshold);
        var verdict = await service.VerifyAsync(CAPTCHAActions.Login, TestValues.NewEmailAddress(), submittedRecaptchaToken, null, TestContext.Current.CancellationToken);
        Assert.True(verdict.Passed);
        Assert.Equal(scoreAtOrAboveThreshold, verdict.Score);
        Assert.False(verdict.MonitorOnly);
    }

    [Fact]
    public async Task VerifyAsync_ScoreBelowThreshold_FailsClosed()
    {
        var scoreBelowThreshold = TestValues.NewScoreBelowDefaultThreshold();
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, _) = CreateService(responseScore: scoreBelowThreshold);
        var verdict = await service.VerifyAsync(CAPTCHAActions.Login, TestValues.NewEmailAddress(), submittedRecaptchaToken, null, TestContext.Current.CancellationToken);
        Assert.False(verdict.Passed);
        Assert.Equal(scoreBelowThreshold, verdict.Score);
        Assert.False(verdict.MonitorOnly);
    }

    [Fact]
    public async Task VerifyAsync_ApiReturnsFalseSuccess_Fails()
    {
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, _) = CreateService(responseScore: TestValues.NewScoreAtOrAboveDefaultThreshold(), success: false);
        var verdict = await service.VerifyAsync(CAPTCHAActions.Login, TestValues.NewEmailAddress(), submittedRecaptchaToken, null, TestContext.Current.CancellationToken);
        Assert.False(verdict.Passed);
        Assert.Equal(0m, verdict.Score);
    }

    [Fact]
    public async Task VerifyAsync_HttpFailure_Fails()
    {
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, _) = CreateService(responseScore: TestValues.NewScoreAtOrAboveDefaultThreshold(), httpStatusCode: HttpStatusCode.ServiceUnavailable);
        var verdict = await service.VerifyAsync(CAPTCHAActions.Login, TestValues.NewEmailAddress(), submittedRecaptchaToken, null, TestContext.Current.CancellationToken);
        Assert.False(verdict.Passed);
        Assert.Equal(0m, verdict.Score);
    }

    [Fact]
    public async Task VerifyAsync_MarkerAndTestEmail_LowScore_PassesMonitorOnly()
    {
        var configuredTestEmail = TestValues.NewEmailAddress();
        var configuredSyntheticMarkerSecret = TestValues.NewSyntheticMarker();
        var scoreBelowThreshold = TestValues.NewScoreBelowDefaultThreshold();
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, _) = CreateService(
            responseScore: scoreBelowThreshold,
            testEmails: [configuredTestEmail],
            syntheticMarkerSecret: configuredSyntheticMarkerSecret);
        var verdict = await service.VerifyAsync(CAPTCHAActions.Login, configuredTestEmail, submittedRecaptchaToken, configuredSyntheticMarkerSecret, TestContext.Current.CancellationToken);
        Assert.True(verdict.Passed);
        Assert.Equal(scoreBelowThreshold, verdict.Score);
        Assert.True(verdict.MonitorOnly);
    }

    [Fact]
    public async Task VerifyAsync_MarkerAndTestEmailDifferentCase_PassesMonitorOnly()
    {
        var configuredTestEmail = TestValues.NewEmailAddress();
        var configuredSyntheticMarkerSecret = TestValues.NewSyntheticMarker();
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, _) = CreateService(
            responseScore: TestValues.NewScoreBelowDefaultThreshold(),
            testEmails: [configuredTestEmail],
            syntheticMarkerSecret: configuredSyntheticMarkerSecret);
        var verdict = await service.VerifyAsync(CAPTCHAActions.Login, configuredTestEmail.ToUpperInvariant(), submittedRecaptchaToken, configuredSyntheticMarkerSecret, TestContext.Current.CancellationToken);
        Assert.True(verdict.Passed);
        Assert.True(verdict.MonitorOnly);
    }

    [Fact]
    public async Task VerifyAsync_MarkerAndTestEmail_HighScore_StillReportsMonitorOnly()
    {
        var configuredTestEmail = TestValues.NewEmailAddress();
        var configuredSyntheticMarkerSecret = TestValues.NewSyntheticMarker();
        var scoreAtOrAboveThreshold = TestValues.NewScoreAtOrAboveDefaultThreshold();
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, _) = CreateService(
            responseScore: scoreAtOrAboveThreshold,
            testEmails: [configuredTestEmail],
            syntheticMarkerSecret: configuredSyntheticMarkerSecret);
        var verdict = await service.VerifyAsync(CAPTCHAActions.Login, configuredTestEmail, submittedRecaptchaToken, configuredSyntheticMarkerSecret, TestContext.Current.CancellationToken);
        Assert.True(verdict.Passed);
        Assert.True(
            verdict.MonitorOnly,
            "a marked-synthetic request is monitor-only regardless of score, or the observed score distribution is censored to failures");
    }

    [Fact]
    public async Task VerifyAsync_MarkerWithoutTestEmail_LowScore_Fails()
    {
        var configuredTestEmail = TestValues.NewEmailAddress();
        var configuredSyntheticMarkerSecret = TestValues.NewSyntheticMarker();
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, _) = CreateService(
            responseScore: TestValues.NewScoreBelowDefaultThreshold(),
            testEmails: [configuredTestEmail],
            syntheticMarkerSecret: configuredSyntheticMarkerSecret);
        var verdict = await service.VerifyAsync(CAPTCHAActions.Login, TestValues.NewEmailAddress(), submittedRecaptchaToken, configuredSyntheticMarkerSecret, TestContext.Current.CancellationToken);
        Assert.False(verdict.Passed);
        Assert.False(verdict.MonitorOnly);
    }

    [Fact]
    public async Task VerifyAsync_TestEmailWithoutMarker_LowScore_Fails()
    {
        var configuredTestEmail = TestValues.NewEmailAddress();
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, _) = CreateService(
            responseScore: TestValues.NewScoreBelowDefaultThreshold(),
            testEmails: [configuredTestEmail],
            syntheticMarkerSecret: TestValues.NewSyntheticMarker());
        var verdict = await service.VerifyAsync(CAPTCHAActions.Login, configuredTestEmail, submittedRecaptchaToken, null, TestContext.Current.CancellationToken);
        Assert.False(
            verdict.Passed,
            "a test email without the marker header must get full enforcement, or the email list becomes a captcha-free credential-stuffing surface");
        Assert.False(verdict.MonitorOnly);
    }

    [Fact]
    public async Task VerifyAsync_WrongMarker_LowScore_Fails()
    {
        var configuredTestEmail = TestValues.NewEmailAddress();
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var presentedWrongMarker = TestValues.NewSyntheticMarker();
        var (service, _) = CreateService(
            responseScore: TestValues.NewScoreBelowDefaultThreshold(),
            testEmails: [configuredTestEmail],
            syntheticMarkerSecret: TestValues.NewSyntheticMarker());
        var verdict = await service.VerifyAsync(CAPTCHAActions.Login, configuredTestEmail, submittedRecaptchaToken, presentedWrongMarker, TestContext.Current.CancellationToken);
        Assert.False(verdict.Passed);
        Assert.False(verdict.MonitorOnly);
    }

    [Fact]
    public async Task VerifyAsync_MarkerSecretNotConfigured_WhitespaceMarkerHeader_Fails()
    {
        var configuredTestEmail = TestValues.NewEmailAddress();
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, _) = CreateService(
            responseScore: TestValues.NewScoreBelowDefaultThreshold(),
            testEmails: [configuredTestEmail],
            syntheticMarkerSecret: null);
        var verdict = await service.VerifyAsync(CAPTCHAActions.Login, configuredTestEmail, submittedRecaptchaToken, " ", TestContext.Current.CancellationToken);
        Assert.False(
            verdict.Passed,
            "an unconfigured marker secret must disable the synthetic path entirely, or blank-matches-blank silently disables the CAPTCHA");
        Assert.False(verdict.MonitorOnly);
    }

    [Fact]
    public async Task VerifyAsync_MonitorOnly_StillCallsSiteverify()
    {
        var configuredTestEmail = TestValues.NewEmailAddress();
        var configuredSyntheticMarkerSecret = TestValues.NewSyntheticMarker();
        var submittedRecaptchaToken = TestValues.NewRecaptchaToken();
        var (service, handlerMock) = CreateService(
            responseScore: TestValues.NewScoreBelowDefaultThreshold(),
            testEmails: [configuredTestEmail],
            syntheticMarkerSecret: configuredSyntheticMarkerSecret);
        await service.VerifyAsync(CAPTCHAActions.Login, configuredTestEmail, submittedRecaptchaToken, configuredSyntheticMarkerSecret, TestContext.Current.CancellationToken);
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    private static (ReCAPTCHAService Service, Mock<HttpMessageHandler> HandlerMock) CreateService(
        decimal responseScore,
        bool success = true,
        HttpStatusCode httpStatusCode = HttpStatusCode.OK,
        bool secretKeyConfigured = true,
        string? syntheticMarkerSecret = null,
        params string[] testEmails)
    {
        var secretKey = secretKeyConfigured ? TestValues.NewRecaptchaSecretKey() : null;
        var renderedScore = responseScore.ToString(CultureInfo.InvariantCulture);
        var json = $$"""{"success":{{(success ? "true" : "false")}},"score":{{renderedScore}}}""";

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = httpStatusCode,
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new UriBuilder("https", "recaptcha.test").Uri
        };
        var optionsMock = new Mock<IOptions<ReCAPTCHAOptions>>(MockBehavior.Strict);
        optionsMock.Setup(o => o.Value).Returns(new ReCAPTCHAOptions
        {
            SecretKey = secretKey,
            TestEmails = testEmails,
            SyntheticMarkerSecret = syntheticMarkerSecret
        });
        return (new ReCAPTCHAService(httpClient, optionsMock.Object), handlerMock);
    }
}
