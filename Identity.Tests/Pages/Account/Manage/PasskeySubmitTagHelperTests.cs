using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Identity.Tests.Pages.Account.Manage;

/// <summary>
/// Tests for PasskeySubmitTagHelper constructor behavior.
/// </summary>
public class PasskeySubmitTagHelperTests
{
    /// <summary>
    /// Verifies that constructing PasskeySubmitTagHelper with a valid IHttpContextAccessor instance
    /// does not throw and initializes publicly observable properties to their declared defaults.
    /// Input conditions:
    /// - httpContextAccessor: a non-null mocked IHttpContextAccessor with either Loose or Strict behavior.
    /// Expected result:
    /// - No exception is thrown.
    /// - The instance is non-null.
    /// - Operation has the enum default value.
    /// - Name is null at runtime (declared with null-forgiving in source).
    /// - EmailName is null.
    /// </summary>
    /// <param name="useStrictMock">If true, create Mock with MockBehavior.Strict; otherwise Loose.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Constructor_ValidHttpContextAccessor_InstanceCreatedAndDefaultsSet(bool useStrictMock)
    {
        // Arrange
        var behavior = useStrictMock ? MockBehavior.Strict : MockBehavior.Loose;
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(behavior);

        // Act
        var helper = new PasskeySubmitTagHelper(httpContextAccessorMock.Object);

        // Assert
        Assert.NotNull(helper);
        // Read into nullable locals to respect nullable annotations in tests.
        PasskeyOperation operation = helper.Operation;
        string? name = helper.Name;
        string? emailName = helper.EmailName;

        Assert.Equal(default(PasskeyOperation), operation);
        Assert.Null(name);
        Assert.Null(emailName);
    }

    /// <summary>
    /// Ensures that multiple independent instances can be created with different IHttpContextAccessor objects
    /// and that each instance maintains independent property defaults.
    /// Input conditions:
    /// - Two different mocked IHttpContextAccessor instances.
    /// Expected result:
    /// - No exception is thrown during construction.
    /// - Each created instance is independent and has the expected default values.
    /// </summary>
    [Fact]
    public void Constructor_DifferentHttpContextAccessors_InstancesAreIndependent()
    {
        // Arrange
        var mockA = new Mock<IHttpContextAccessor>(MockBehavior.Loose);
        var mockB = new Mock<IHttpContextAccessor>(MockBehavior.Loose);

        // Act
        var helperA = new PasskeySubmitTagHelper(mockA.Object);
        var helperB = new PasskeySubmitTagHelper(mockB.Object);

        // Assert
        Assert.NotSame(helperA, helperB);

        // Verify defaults for helperA
        PasskeyOperation opA = helperA.Operation;
        string? nameA = helperA.Name;
        string? emailA = helperA.EmailName;

        Assert.Equal(default(PasskeyOperation), opA);
        Assert.Null(nameA);
        Assert.Null(emailA);

        // Verify defaults for helperB
        PasskeyOperation opB = helperB.Operation;
        string? nameB = helperB.Name;
        string? emailB = helperB.EmailName;

        Assert.Equal(default(PasskeyOperation), opB);
        Assert.Null(nameB);
        Assert.Null(emailB);
    }

    /// <summary>
    /// Verifies that when no IAntiforgery service is available (tokens == null)
    /// and EmailName is null, the output HTML contains an empty request token name/value
    /// and email-name is rendered as an empty string. Also ensures attributes meant
    /// for the button are written and 'operation','name','email-name' are not included
    /// as button attributes.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_TokensNull_EmailNameNull_EmitsEmptyTokenAttributes()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        // RequestServices has no IAntiforgery registered -> GetService returns null
        httpContext.RequestServices = new ServiceCollection().BuildServiceProvider();

        var httpAccessorMock = new Mock<IHttpContextAccessor>();
        httpAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var helper = new PasskeySubmitTagHelper(httpAccessorMock.Object)
        {
            Operation = (PasskeyOperation)0,
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

        var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), Guid.NewGuid().ToString());

        // Act
        await helper.ProcessAsync(context, output);

        // Assert
        Assert.Null(output.TagName); // TagName cleared
        Assert.Empty(output.Attributes); // Attributes cleared
        string html = output.Content.GetContent(NullHtmlEncoder.Default);

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