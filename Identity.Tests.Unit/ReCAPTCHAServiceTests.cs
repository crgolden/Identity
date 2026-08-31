namespace Identity.Tests.Unit;

using System.Net;
using CAPTCHA;
using Identity;
using Infrastructure;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ReCAPTCHAServiceTests
{
    [Fact]
    public async Task VerifyAsync_NullToken_ReturnsZero()
    {
        var service = CreateService(responseScore: 0.9m);
        var result = await service.VerifyAsync(null, TestContext.Current.CancellationToken);
        Assert.Equal(0m, result);
    }

    [Fact]
    public async Task VerifyAsync_EmptyToken_ReturnsZero()
    {
        var service = CreateService(responseScore: 0.9m);
        var result = await service.VerifyAsync(string.Empty, TestContext.Current.CancellationToken);
        Assert.Equal(0m, result);
    }

    [Fact]
    public async Task VerifyAsync_NullSecretKey_ReturnsZero()
    {
        var service = CreateService(responseScore: 0.9m, secretKey: null);
        var result = await service.VerifyAsync("valid-token", TestContext.Current.CancellationToken);
        Assert.Equal(0m, result);
    }

    [Fact]
    public async Task VerifyAsync_SuccessResponseHighScore_ReturnsScore()
    {
        var service = CreateService(responseScore: 0.9m);
        var result = await service.VerifyAsync("valid-token", TestContext.Current.CancellationToken);
        Assert.Equal(0.9m, result);
    }

    [Fact]
    public async Task VerifyAsync_SuccessResponseLowScore_ReturnsScore()
    {
        var service = CreateService(responseScore: 0.1m);
        var result = await service.VerifyAsync("valid-token", TestContext.Current.CancellationToken);
        Assert.Equal(0.1m, result);
    }

    [Fact]
    public async Task VerifyAsync_ApiReturnsFalseSuccess_ReturnsZero()
    {
        var service = CreateService(responseScore: 0.9m, success: false);
        var result = await service.VerifyAsync("valid-token", TestContext.Current.CancellationToken);
        Assert.Equal(0m, result);
    }

    [Fact]
    public async Task VerifyAsync_HttpFailure_ReturnsZero()
    {
        var service = CreateService(responseScore: 0.9m, httpStatusCode: HttpStatusCode.ServiceUnavailable);
        var result = await service.VerifyAsync("valid-token", TestContext.Current.CancellationToken);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void IsExempt_AdminEmail_ReturnsTrue()
    {
        var configuredAdminEmail = TestValues.NewEmailAddress();
        var service = CreateService(responseScore: 0.9m, adminEmail: configuredAdminEmail);
        Assert.True(service.IsExempt(configuredAdminEmail));
    }

    [Fact]
    public void IsExempt_AdminEmailCaseInsensitive_ReturnsTrue()
    {
        var configuredAdminEmail = TestValues.NewEmailAddress();
        var service = CreateService(responseScore: 0.9m, adminEmail: configuredAdminEmail);
        Assert.True(service.IsExempt(configuredAdminEmail.ToUpperInvariant()));
    }

    [Fact]
    public void IsExempt_TestEmail_ReturnsTrue()
    {
        var configuredTestEmail = TestValues.NewEmailAddress();
        var service = CreateService(responseScore: 0.9m, testEmail: configuredTestEmail);
        Assert.True(service.IsExempt(configuredTestEmail));
    }

    [Fact]
    public void IsExempt_NullEmail_ReturnsFalse()
    {
        var service = CreateService(responseScore: 0.9m, adminEmail: TestValues.NewEmailAddress());
        Assert.False(service.IsExempt(null));
    }

    [Fact]
    public void IsExempt_UnknownEmail_ReturnsFalse()
    {
        var service = CreateService(
            responseScore: 0.9m,
            adminEmail: TestValues.NewEmailAddress(),
            testEmail: TestValues.NewEmailAddress());
        Assert.False(service.IsExempt(TestValues.NewEmailAddress()));
    }

    [Fact]
    public void IsExempt_TestEmailNotConfigured_ReturnsFalseForEveryCaller()
    {
        var service = CreateService(responseScore: 0.9m, adminEmail: TestValues.NewEmailAddress(), testEmail: null);
        Assert.False(
            service.IsExempt(TestValues.NewEmailAddress()),
            "an unconfigured TestEmail must exempt nobody, or a null option silently disables the CAPTCHA");
    }

    [Fact]
    public void IsExempt_AdminEmailNotConfigured_ReturnsFalseForEveryCaller()
    {
        var service = CreateService(responseScore: 0.9m, adminEmail: null, testEmail: TestValues.NewEmailAddress());
        Assert.False(
            service.IsExempt(TestValues.NewEmailAddress()),
            "an unconfigured AdminEmail must exempt nobody, or a null option silently disables the CAPTCHA");
    }

    private static ReCAPTCHAService CreateService(
        decimal responseScore,
        bool success = true,
        HttpStatusCode httpStatusCode = HttpStatusCode.OK,
        string? secretKey = "test-secret",
        string? adminEmail = null,
        string? testEmail = null)
    {
        var json = $$"""{"success":{{(success ? "true" : "false")}},"score":{{responseScore}}}""";

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
        optionsMock.Setup(o => o.Value).Returns(new ReCAPTCHAOptions { SecretKey = secretKey, AdminEmail = adminEmail, TestEmail = testEmail });
        return new ReCAPTCHAService(httpClient, optionsMock.Object);
    }
}