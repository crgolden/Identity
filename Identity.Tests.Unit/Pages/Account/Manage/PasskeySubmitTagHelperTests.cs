namespace Identity.Tests.Unit.Pages.Account.Manage;

using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class PasskeySubmitTagHelperTests
{
    [Fact]
    public void Constructor_ValidDependencies_InitializesDefaults()
    {
        // Arrange
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);

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
    public async Task ProcessAsync_AntiforgeryTokensAreAbsent_OmitsTheTokenAttributes()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        var httpAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var antiforgeryMock = new Mock<IAntiforgery>(MockBehavior.Strict);
        antiforgeryMock
            .Setup(a => a.GetTokens(httpContext))
            .Returns(new AntiforgeryTokenSet(null, Generated.NewSessionKey(), Generated.NewPropertyKey(), null));

        var helper = new PasskeySubmitTagHelper(httpAccessorMock.Object, antiforgeryMock.Object)
        {
            Operation = 0,
            Name = Generated.NewApiResourceName()
        };

        var passThroughName = Generated.NewAttributeName();
        var passThroughValue = Generated.NewAttributeValue();
        var attributes = new TagHelperAttributeList
            {
                new TagHelperAttribute(PasskeySubmitTagHelper.OperationAttributeName, Generated.NewAttributeValue()),
                new TagHelperAttribute(PasskeySubmitTagHelper.NameAttributeName, Generated.NewAttributeValue()),
                new TagHelperAttribute(PasskeySubmitTagHelper.EmailNameAttributeName, Generated.NewAttributeValue()),
                new TagHelperAttribute(passThroughName, passThroughValue)
            };

        var buttonLabel = Generated.NewButtonLabel();
        var childContent = new DefaultTagHelperContent();
        childContent.SetContent(buttonLabel);

        var output = new TagHelperOutput(
            PasskeySubmitTagHelper.TagName,
            attributes,
            (_, _) => Task.FromResult<TagHelperContent>(childContent));

        var uniqueId = Guid.NewGuid().ToString();
        var context = new TagHelperContext([], new Dictionary<object, object>(), uniqueId);

        // Act
        await helper.ProcessAsync(context, output);

        // Assert
        Assert.Null(output.TagName);
        Assert.Empty(output.Attributes);
        var html = output.Content.GetContent(NullHtmlEncoder.Default);
        Assert.Contains(PasskeySubmitTagHelper.ButtonOpeningTag, html, StringComparison.Ordinal);
        Assert.Contains($"{passThroughName}=\"{passThroughValue}\"", html, StringComparison.Ordinal);
        Assert.Contains(buttonLabel + PasskeySubmitTagHelper.ButtonClosingTag, html, StringComparison.Ordinal);
        Assert.Contains(
            $"{PasskeySubmitTagHelper.OperationAttributeName}=\"{helper.Operation}\" ",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            $"{PasskeySubmitTagHelper.NameAttributeName}=\"{helper.Name}\" ",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            $"{PasskeySubmitTagHelper.AutofillAttributeName}=\"{PasskeySubmitTagHelper.AutofillOn}\" ",
            html,
            StringComparison.Ordinal);
        Assert.DoesNotContain(PasskeySubmitTagHelper.EmailNameAttributeName, html, StringComparison.Ordinal);
        Assert.DoesNotContain(PasskeySubmitTagHelper.RequestTokenNameAttributeName, html, StringComparison.Ordinal);
        Assert.DoesNotContain(PasskeySubmitTagHelper.RequestTokenValueAttributeName, html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true, PasskeySubmitTagHelper.AutofillOn)]
    [InlineData(false, PasskeySubmitTagHelper.AutofillOff)]
    public async Task ProcessAsync_RendersTheAutofillState_SoAFailedAttemptDoesNotRetryItself(
        bool autofill,
        string expected)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        var httpAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var antiforgeryMock = new Mock<IAntiforgery>(MockBehavior.Strict);
        antiforgeryMock
            .Setup(a => a.GetTokens(httpContext))
            .Returns(new AntiforgeryTokenSet(null, Generated.NewSessionKey(), Generated.NewPropertyKey(), null));

        var helper = new PasskeySubmitTagHelper(httpAccessorMock.Object, antiforgeryMock.Object)
        {
            Operation = 0,
            Name = Generated.NewApiResourceName(),
            Autofill = autofill
        };

        var suppressedAutofillValue = Generated.NewAttributeValue();
        var attributes = new TagHelperAttributeList
            {
                new TagHelperAttribute(PasskeySubmitTagHelper.AutofillAttributeName, suppressedAutofillValue)
            };

        var childContent = new DefaultTagHelperContent();
        childContent.SetContent(Generated.NewClaimValue());

        var output = new TagHelperOutput(
            PasskeySubmitTagHelper.TagName,
            attributes,
            (_, _) => Task.FromResult<TagHelperContent>(childContent));

        var uniqueId = Guid.NewGuid().ToString();
        var context = new TagHelperContext([], new Dictionary<object, object>(), uniqueId);

        // Act
        await helper.ProcessAsync(context, output);

        // Assert
        var html = output.Content.GetContent(NullHtmlEncoder.Default);
        Assert.Contains(
            $"{PasskeySubmitTagHelper.AutofillAttributeName}=\"{expected}\" ",
            html,
            StringComparison.Ordinal);
        Assert.DoesNotContain(suppressedAutofillValue, html, StringComparison.Ordinal);
    }
}
