namespace Identity.Tests.Pages.Account.Manage;
using Infrastructure;

using Identity.Pages.Account.Manage;
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

        // Read into nullable locals to respect nullable annotations in tests.
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

        // Verify defaults for helperA
        var opA = helperA.Operation;
        var nameA = helperA.Name;
        var emailA = helperA.EmailName;

        Assert.Null(opA);
        Assert.Null(nameA);
        Assert.Null(emailA);

        // Verify defaults for helperB
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

        // Prepare attributes: include ones that should be filtered out and one that should be included
        var attributes = new TagHelperAttributeList
            {
                new TagHelperAttribute("operation", "op-should-be-ignored"),
                new TagHelperAttribute("name", "name-should-be-ignored"),
                new TagHelperAttribute("email-name", "email-should-be-ignored"),
                new TagHelperAttribute("class", "btn-primary")
            };

        // Child content non-empty to ensure button content will be present
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
        Assert.Null(output.TagName); // TagName cleared
        Assert.Empty(output.Attributes); // Attributes cleared
        var html = output.Content.GetContent(NullHtmlEncoder.Default);

        // Contains button with submitted class attribute and inner content
        Assert.Contains("<button", html, StringComparison.Ordinal);
        Assert.Contains("class=\"btn-primary\"", html, StringComparison.Ordinal);
        Assert.Contains(">ClickMe</button>", html, StringComparison.Ordinal);

        // passkey-submit element contains operation and provided Name
        Assert.Contains($"operation=\"{helper.Operation}\"", html, StringComparison.Ordinal);
        Assert.Contains($"name=\"{helper.Name}\"", html, StringComparison.Ordinal);

        // EmailName was null -> rendered as empty attribute value
        Assert.Contains("email-name=\"\"", html, StringComparison.Ordinal);

        // No antiforgery tokens -> request-token-name and value empty
        Assert.Contains("request-token-name=\"\"", html, StringComparison.Ordinal);
        Assert.Contains("request-token-value=\"\"", html, StringComparison.Ordinal);
    }
}