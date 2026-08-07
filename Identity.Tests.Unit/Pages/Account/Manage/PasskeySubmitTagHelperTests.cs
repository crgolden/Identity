namespace Identity.Tests.Unit.Pages.Account.Manage;

using Identity.Pages.Account.Manage;
using Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class PasskeySubmitTagHelperTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Constructor_ValidDependencies_InitializesDefaults(bool useStrictMock)
    {
        // Arrange
        var behavior = useStrictMock ? MockBehavior.Strict : MockBehavior.Loose;
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(behavior);

        // Act
        var helper = new PasskeySubmitTagHelper(httpContextAccessorMock.Object, Mock.Of<IAntiforgery>());

        // Assert
        Assert.NotNull(helper);
        var operation = helper.Operation;
        var name = helper.Name;
        var emailName = helper.EmailName;

        Assert.Null(operation);
        Assert.Null(name);
        Assert.Null(emailName);
    }

    [Fact]
    public void Constructor_DifferentAccessors_CreatesIndependentInstances()
    {
        // Arrange
        var mockA = new Mock<IHttpContextAccessor>(MockBehavior.Loose);
        var mockB = new Mock<IHttpContextAccessor>(MockBehavior.Loose);

        // Act
        var helperA = new PasskeySubmitTagHelper(mockA.Object, Mock.Of<IAntiforgery>());
        var helperB = new PasskeySubmitTagHelper(mockB.Object, Mock.Of<IAntiforgery>());

        // Assert
        Assert.NotSame(helperA, helperB);
        var opA = helperA.Operation;
        var nameA = helperA.Name;
        var emailA = helperA.EmailName;

        Assert.Null(opA);
        Assert.Null(nameA);
        Assert.Null(emailA);
        var opB = helperB.Operation;
        var nameB = helperB.Name;
        var emailB = helperB.EmailName;

        Assert.Null(opB);
        Assert.Null(nameB);
        Assert.Null(emailB);
    }

    [Fact]
    public async Task ProcessAsync_NullAntiforgeryTokens_EmitsEmptyTokenAttributes()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        var httpAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var antiforgeryMock = new Mock<IAntiforgery>(MockBehavior.Strict);
        antiforgeryMock
            .Setup(a => a.GetTokens(httpContext))
            .Returns(new AntiforgeryTokenSet(null, "cookie", "__RequestVerificationToken", null));

        var helper = new PasskeySubmitTagHelper(httpAccessorMock.Object, antiforgeryMock.Object)
        {
            Operation = 0,
            Name = "theName",
            EmailName = null
        };

        var attributes = new TagHelperAttributeList
            {
                new TagHelperAttribute("operation", "op-should-be-ignored"),
                new TagHelperAttribute("name", "name-should-be-ignored"),
                new TagHelperAttribute("email-name", "email-should-be-ignored"),
                new TagHelperAttribute("class", "btn-primary")
            };

        var childContent = new DefaultTagHelperContent();
        childContent.SetContent("ClickMe");

        var output = new TagHelperOutput(
            "passkey-submit",
            attributes,
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(childContent));

        var context = new TagHelperContext([], new Dictionary<object, object>(), Guid.NewGuid().ToString());

        // Act
        await helper.ProcessAsync(context, output);

        // Assert
        Assert.Null(output.TagName);
        Assert.Empty(output.Attributes);
        var html = output.Content.GetContent(NullHtmlEncoder.Default);
        Assert.Contains("<button", html, StringComparison.Ordinal);
        Assert.Contains("class=\"btn-primary\"", html, StringComparison.Ordinal);
        Assert.Contains(">ClickMe</button>", html, StringComparison.Ordinal);
        Assert.Contains($"operation=\"{helper.Operation}\"", html, StringComparison.Ordinal);
        Assert.Contains($"name=\"{helper.Name}\"", html, StringComparison.Ordinal);
        Assert.Contains("email-name=\"\"", html, StringComparison.Ordinal);
        Assert.Contains("request-token-name=\"\"", html, StringComparison.Ordinal);
        Assert.Contains("request-token-value=\"\"", html, StringComparison.Ordinal);
    }
}