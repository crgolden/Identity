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
    public async Task ProcessAsync_NullAntiforgeryTokens_EmitsEmptyTokenAttributes()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        var httpAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        httpAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var antiforgeryMock = new Mock<IAntiforgery>(MockBehavior.Strict);
        antiforgeryMock
            .Setup(a => a.GetTokens(httpContext))
            .Returns(new AntiforgeryTokenSet(null, TestValues.NewSessionKey(), TestValues.NewPropertyKey(), null));

        var helper = new PasskeySubmitTagHelper(httpAccessorMock.Object, antiforgeryMock.Object)
        {
            Operation = 0,
            Name = TestValues.NewApiResourceName(),
            EmailName = null
        };

        var passThroughName = TestValues.NewAttributeName();
        var passThroughValue = TestValues.NewAttributeValue();
        var attributes = new TagHelperAttributeList
            {
                new TagHelperAttribute(PasskeySubmitTagHelper.OperationAttributeName, TestValues.NewAttributeValue()),
                new TagHelperAttribute(PasskeySubmitTagHelper.NameAttributeName, TestValues.NewAttributeValue()),
                new TagHelperAttribute(PasskeySubmitTagHelper.EmailNameAttributeName, TestValues.NewAttributeValue()),
                new TagHelperAttribute(passThroughName, passThroughValue)
            };

        var buttonLabel = TestValues.NewButtonLabel();
        var childContent = new DefaultTagHelperContent();
        childContent.SetContent(buttonLabel);

        var output = new TagHelperOutput(
            PasskeySubmitTagHelper.TagName,
            attributes,
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(childContent));

        var uniqueId = Guid.NewGuid().ToString();
        var context = new TagHelperContext([], new Dictionary<object, object>(), uniqueId);

        // Act
        await helper.ProcessAsync(context, output);

        // Assert
        Assert.Null(output.TagName);
        Assert.Empty(output.Attributes);
        var html = output.Content.GetContent(NullHtmlEncoder.Default);
        Assert.Contains(PasskeySubmitTagHelper.ButtonOpeningTag, html, StringComparison.Ordinal);
        Assert.Contains(
            PasskeySubmitTagHelper.Attribute(passThroughName, passThroughValue),
            html,
            StringComparison.Ordinal);
        Assert.Contains(buttonLabel + PasskeySubmitTagHelper.ButtonClosingTag, html, StringComparison.Ordinal);
        Assert.Contains(
            PasskeySubmitTagHelper.Attribute(PasskeySubmitTagHelper.OperationAttributeName, helper.Operation?.ToString()),
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            PasskeySubmitTagHelper.Attribute(PasskeySubmitTagHelper.NameAttributeName, helper.Name),
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            PasskeySubmitTagHelper.Attribute(PasskeySubmitTagHelper.EmailNameAttributeName, null),
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            PasskeySubmitTagHelper.Attribute(PasskeySubmitTagHelper.RequestTokenNameAttributeName, null),
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            PasskeySubmitTagHelper.Attribute(PasskeySubmitTagHelper.RequestTokenValueAttributeName, null),
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            PasskeySubmitTagHelper.Attribute(PasskeySubmitTagHelper.AutofillAttributeName, PasskeySubmitTagHelper.AutofillOn),
            html,
            StringComparison.Ordinal);
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
            .Returns(new AntiforgeryTokenSet(null, TestValues.NewSessionKey(), TestValues.NewPropertyKey(), null));

        var helper = new PasskeySubmitTagHelper(httpAccessorMock.Object, antiforgeryMock.Object)
        {
            Operation = 0,
            Name = TestValues.NewApiResourceName(),
            Autofill = autofill
        };

        var suppressedAutofillValue = TestValues.NewAttributeValue();
        var attributes = new TagHelperAttributeList
            {
                new TagHelperAttribute(PasskeySubmitTagHelper.AutofillAttributeName, suppressedAutofillValue)
            };

        var childContent = new DefaultTagHelperContent();
        childContent.SetContent(TestValues.NewClaimValue());

        var output = new TagHelperOutput(
            PasskeySubmitTagHelper.TagName,
            attributes,
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(childContent));

        var uniqueId = Guid.NewGuid().ToString();
        var context = new TagHelperContext([], new Dictionary<object, object>(), uniqueId);

        // Act
        await helper.ProcessAsync(context, output);

        // Assert
        var html = output.Content.GetContent(NullHtmlEncoder.Default);
        Assert.Contains(
            PasskeySubmitTagHelper.Attribute(PasskeySubmitTagHelper.AutofillAttributeName, expected),
            html,
            StringComparison.Ordinal);
        Assert.DoesNotContain(suppressedAutofillValue, html, StringComparison.Ordinal);
    }
}