namespace Identity.Tests;

using System.Net;
using System.Net.Http.Json;
using Identity;
using Identity.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
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

    private static ReCAPTCHAService CreateService(
        decimal responseScore,
        bool success = true,
        HttpStatusCode httpStatusCode = HttpStatusCode.OK,
        string? secretKey = "test-secret")
    {
        var json = $$"""{"success":{{(success ? "true" : "false")}},"score":{{responseScore}}}""";

        var handlerMock = new Mock<HttpMessageHandler>();
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
        var optionsMock = new Mock<IOptions<ReCAPTCHAOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new ReCAPTCHAOptions { SecretKey = secretKey });
        return new ReCAPTCHAService(httpClient, optionsMock.Object, NullLogger<ReCAPTCHAService>.Instance);
    }
}
