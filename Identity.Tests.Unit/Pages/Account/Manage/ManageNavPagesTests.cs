namespace Identity.Tests.Unit.Pages.Account.Manage;

using Identity.Pages.Account.Manage;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ManageNavPagesTests
{
    public static TheoryData<object?, bool, string?, string?> DeletePersonalDataCases() => new()
    {
        { "DeletePersonalData", true, "/some/path/Irrelevant.cshtml", "active" },
        { "deletepersonaldata", true, "/some/path/Irrelevant.cshtml", "active" },
        { "SomethingElse", true, "/Areas/Identity/Pages/Account/Manage/DeletePersonalData.cshtml", null },
        { null, false, "/Areas/Identity/Pages/Account/Manage/DeletePersonalData.cshtml", "active" },
        { 123, true, "/Areas/Identity/Pages/Account/Manage/DeletePersonalData.cshtml", "active" },
        { null, false, null, null },
    };

    public static TheoryData<object?, string?, string, string?> PageNavTestData()
    {
        var longStr = new string('a', 600);
        return new TheoryData<object?, string?, string, string?>
        {
            { "Index", "/Areas/Identity/Pages/Account/Manage/Index.cshtml", "Index", "active" },
            { "index", "/Some/Path/Index.cshtml", "Index", "active" },
            { string.Empty, "/Any/Path/Ignore.cshtml", string.Empty, "active" },
            { null, "/Areas/Identity/Pages/Account/Manage/ChangePassword.cshtml", "ChangePassword", "active" },
            { 123, "/some/path/Custom-Page.cshtml", "Custom-Page", "active" },
            { "OtherPage", "/path/Index.cshtml", "Index", null },
            { null, null, "Index", null },
            { null, "/x/y/special_name!@#$.cshtml", "special_name!@#$", "active" },
            { "   ", "/ignored/path.cshtml", "   ", "active" },
            { longStr, "/ignored/long.cshtml", longStr, "active" },
            { null, "PlainName.cshtml", "PlainName", "active" },
        };
    }

    public static TheoryData<object?, string?, string?> EmailNavClassCases() => new()
    {
        { "Email", null, "active" },
        { "email", null, "active" },
        { "Other", null, null },
        { null, "/Pages/Account/Manage/Email.cshtml", "active" },
        { null, "/Pages/Account/Manage/Other.cshtml", null },
        { 123, "/Pages/Account/Manage/Email.cshtml", "active" },
        { null, null, null },
        { null, "/Pages/Account/Manage/EMAIL.CSHTML", "active" },
        { "   ", "/Pages/Account/Manage/Email.cshtml", null },
    };

    public static TheoryData<string?, string?, string?> PageCases() => new()
    {
        { "ChangePassword", null, "active" },
        { "changepassword", null, "active" },
        { null, "/Views/Account/Manage/ChangePassword.cshtml", "active" },
        { null, "/Views/Account/Manage/Other.cshtml", null },
        { null, null, null },
    };

    public static TheoryData<string?, string?, string?> GetPersonalDataNavCases() => new()
    {
        { "PersonalData", null, "active" },
        { "personaldata", null, "active" },
        { null, "Pages/Account/Manage/PersonalData.cshtml", "active" },
        { null, "C:\\Views\\Account\\Manage\\PersonalData.cshtml", "active" },
        { null, null, null },
        { string.Empty, "Pages/Account/Manage/PersonalData.cshtml", null },
        { "   ", null, null },
        { "PersonalData ", null, null },
        { new string('x', 1000), null, null },
        { "PersonalData\u2603", null, null },
        { null, "Pages/Account/Manage/some.PersonalData.cshtml", null },
    };

    [Theory]
    [InlineData("Index")]
    public void Index_Property_ReturnsExpected(string expected)
    {
        // Arrange

        // Act
        var result = ManageNavPages.Index;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expected, result);
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData("ExternalLogins")]
    public void ExternalLogins_Property_ReturnsExpected(string expected)
    {
        // Arrange

        // Act
        var actual = ManageNavPages.ExternalLogins;

        // Assert
        Assert.NotNull(actual);
        Assert.False(string.IsNullOrWhiteSpace(actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("Index")]
    [InlineData("index")]
    [InlineData("INDEX")]
    public void IndexNavClass_ActivePageMatches_ReturnsActive(string activePage)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var actionDescriptor = new ActionDescriptor { DisplayName = "/Pages/Account/Manage/Index.cshtml" };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);

        var metadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary())
        {
            ["ActivePage"] = activePage
        };

        var mockView = new Mock<IView>(MockBehavior.Strict);
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        var viewContext = new ViewContext(actionContext, mockView.Object, viewData, tempData, TextWriter.Null, new HtmlHelperOptions());

        // Act
        var result = ManageNavPages.IndexNavClass(viewContext);

        // Assert
        Assert.Equal("active", result);
    }

    [Theory]
    [InlineData("Index.cshtml")]
    [InlineData("index.cshtml")]
    [InlineData("/Areas/Identity/Pages/Account/Manage/Index.cshtml")]
    [InlineData("Index")]
    public void IndexNavClass_NullActivePage_UsesDisplayNameFilename_ReturnsActive(string displayName)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var actionDescriptor = new ActionDescriptor { DisplayName = displayName };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);

        var metadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary());
        var mockView = new Mock<IView>(MockBehavior.Strict);
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        var viewContext = new ViewContext(actionContext, mockView.Object, viewData, tempData, TextWriter.Null, new HtmlHelperOptions());

        // Act
        var result = ManageNavPages.IndexNavClass(viewContext);

        // Assert
        Assert.Equal("active", result);
    }

    [Theory]
    [InlineData("Email", "/Pages/Account/Manage/Email.cshtml")]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public void IndexNavClass_NoMatch_ReturnsNull(string? activePage, string? displayName)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var actionDescriptor = new ActionDescriptor { DisplayName = displayName };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);

        var metadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary());
        if (activePage is not null)
        {
            viewData["ActivePage"] = activePage;
        }

        var mockView = new Mock<IView>(MockBehavior.Strict);
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        var viewContext = new ViewContext(actionContext, mockView.Object, viewData, tempData, TextWriter.Null, new HtmlHelperOptions());

        // Act
        var result = ManageNavPages.IndexNavClass(viewContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void IndexNavClass_NonStringActivePage_FallsBackToDisplayName()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var actionDescriptor = new ActionDescriptor { DisplayName = "Index.cshtml" };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);

        var metadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary())
        {
            ["ActivePage"] = 123
        };

        var mockView = new Mock<IView>(MockBehavior.Strict);
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        var viewContext = new ViewContext(actionContext, mockView.Object, viewData, tempData, TextWriter.Null, new HtmlHelperOptions());

        // Act
        var result = ManageNavPages.IndexNavClass(viewContext);

        // Assert
        Assert.Equal("active", result);
    }

#pragma warning disable xUnit1045
    [Theory]
    [MemberData(nameof(DeletePersonalDataCases))]
    public void DeletePersonalDataNavClass_VariousViewContexts_ReturnsExpected(object? activePageValue, bool hasActivePage, string? displayName, string? expected)
    {
        // Arrange
        var viewContext = new ViewContext
        {
            ActionDescriptor = new ActionDescriptor { DisplayName = displayName },
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        };

        if (hasActivePage)
        {
            viewContext.ViewData["ActivePage"] = activePageValue;
        }

        // Act
        var result = ManageNavPages.DeletePersonalDataNavClass(viewContext);

        // Assert
        if (expected is null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.Equal(expected, result);
        }
    }
#pragma warning restore xUnit1045

    [Fact]
    public void DeletePersonalDataNavClass_NullViewContext_ThrowsNullReferenceException()
    {
        // Arrange
        ViewContext? viewContext = null;

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => ManageNavPages.DeletePersonalDataNavClass(viewContext!));
    }

#pragma warning disable xUnit1045
    [Theory]
    [MemberData(nameof(PageNavTestData))]
    public void PageNavClass_VariousInputs_ReturnsExpected(object? activePage, string? displayName, string page, string? expected)
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        viewData["ActivePage"] = activePage;
        var viewContext = new ViewContext
        {
            ViewData = viewData,
            ActionDescriptor = new ActionDescriptor { DisplayName = displayName }
        };

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, page);

        // Assert
        Assert.Equal(expected, result);
    }
#pragma warning restore xUnit1045

    [Theory]
    [InlineData("DownloadPersonalData")]
    public void DownloadPersonalData_Property_ReturnsExpected(string expected)
    {
        // Arrange

        // Act
        var result = ManageNavPages.DownloadPersonalData;

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("PersonalData", 12)]
    public void PersonalData_Property_ReturnsExpected(string expected, int expectedLength)
    {
        // Arrange

        // Act
        var actual = ManageNavPages.PersonalData;

        // Assert
        Assert.NotNull(actual);
        Assert.NotEmpty(actual);
        Assert.Equal(expected, actual);
        Assert.Equal(expectedLength, actual.Length);
        Assert.DoesNotContain(" ", actual);
        Assert.DoesNotContain("\t", actual);
        Assert.DoesNotContain("\n", actual);
        Assert.DoesNotContain("\r", actual);
        foreach (var c in actual)
        {
            Assert.False(char.IsControl(c), $"Unexpected control character U+{(int)c:X4} in PersonalData value.");
        }
    }

    [Fact]
    public void PersonalData_Property_IsStableAcrossAccesses()
    {
        // Arrange

        // Act
        var first = ManageNavPages.PersonalData;
        var second = ManageNavPages.PersonalData;
        var third = ManageNavPages.PersonalData;

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(second, third);
        Assert.True(ReferenceEquals(first, second));
    }

#pragma warning disable xUnit1045
    [Theory]
    [MemberData(nameof(EmailNavClassCases))]
    public void EmailNavClass_VariousActivePageAndDisplayName_ReturnsExpected(object? activePageValue, string? displayName, string? expected)
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        if (activePageValue != null)
        {
            viewData["ActivePage"] = activePageValue;
        }

        var actionDescriptor = new ActionDescriptor
        {
            DisplayName = displayName
        };

        var viewContext = new ViewContext
        {
            ViewData = viewData,
            ActionDescriptor = actionDescriptor
        };

        // Act
        var result = ManageNavPages.EmailNavClass(viewContext);

        // Assert
        if (expected is null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.Equal(expected, result);
        }
    }
#pragma warning restore xUnit1045

    [Theory]
    [InlineData("ExternalLogins", null, "active")]
    [InlineData("externallogins", null, "active")]
    [InlineData(null, "Areas/Identity/Pages/Account/Manage/ExternalLogins.cshtml", "active")]
    [InlineData(null, "Areas/Identity/Pages/Account/Manage/SomeOther.cshtml", null)]
    [InlineData("", "Areas/Identity/Pages/Account/Manage/ExternalLogins.cshtml", null)]
    [InlineData("  ExternalLogins  ", null, null)]
    public void ExternalLoginsNavClass_VariousActivePageAndDisplayName_ReturnsExpected(string? activePage, string? displayName, string? expected)
    {
        // Arrange
        var viewContext = CreateViewContext(activePage, displayName);

        // Act
        var result = ManageNavPages.ExternalLoginsNavClass(viewContext);

        // Assert
        if (expected is null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.Equal(expected, result);
        }
    }

    [Fact]
    public void ExternalLoginsNavClass_NullViewContext_ThrowsNullReferenceException()
    {
        // Arrange
        ViewContext? viewContext = null;

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => ManageNavPages.ExternalLoginsNavClass(viewContext!));
    }

    [Theory]
    [InlineData("ChangePassword")]
    public void ChangePassword_Property_ReturnsExpected(string expected)
    {
        // Arrange

        // Act
        var actual = ManageNavPages.ChangePassword;

        // Assert
        Assert.NotNull(actual);
        Assert.NotEmpty(actual);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ChangePassword_Property_IsStableAcrossAccesses()
    {
        // Arrange
        // Act
        var first = ManageNavPages.ChangePassword;
        var second = ManageNavPages.ChangePassword;

        // Assert
        Assert.Equal(first, second);
        Assert.NotNull(first);
        Assert.NotEmpty(first);
    }

    [Theory]
    [InlineData("TwoFactorAuthentication")]
    public void TwoFactorAuthentication_Property_ReturnsExpected(string expected)
    {
        // Arrange

        // Act
        var result = ManageNavPages.TwoFactorAuthentication;

        // Assert
        Assert.Equal(expected, result);
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Theory]
    [MemberData(nameof(PageCases))]
    public void ChangePasswordNavClass_VariousActivePageAndDisplayName_ReturnsExpected(string? activePage, string? displayName, string? expected)
    {
        // Arrange
        var actionDescriptor = new ActionDescriptor { DisplayName = displayName };
        var httpContext = new DefaultHttpContext();
        var routeData = new RouteData();
        var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        if (activePage != null)
        {
            viewData["ActivePage"] = activePage;
        }

        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        var viewMock = new Mock<IView>(MockBehavior.Strict);
        var view = viewMock.Object;
        var writer = new StringWriter();
        var htmlHelperOptions = new HtmlHelperOptions();

        var viewContext = new ViewContext(actionContext, view, viewData, tempData, writer, htmlHelperOptions);

        // Act
        var result = ManageNavPages.ChangePasswordNavClass(viewContext);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ChangePasswordNavClass_WhitespaceActivePage_ReturnsNull()
    {
        // Arrange
        var actionDescriptor = new ActionDescriptor { DisplayName = null };
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            ["ActivePage"] = "   "
        };

        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        var view = new Mock<IView>(MockBehavior.Strict).Object;
        var viewContext = new ViewContext(actionContext, view, viewData, tempData, new StringWriter(), new HtmlHelperOptions());

        // Act
        var result = ManageNavPages.ChangePasswordNavClass(viewContext);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [MemberData(nameof(GetPersonalDataNavCases))]
    public void PersonalDataNavClass_VariousActivePageAndDisplayName_ReturnsExpected(string? activePage, string? actionDisplayName, string? expected)
    {
        // Arrange
        var viewContext = CreateViewContext(activePage, actionDisplayName);

        // Act
        var result = ManageNavPages.PersonalDataNavClass(viewContext);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Passkeys")]
    [InlineData("passkeys")]
    [InlineData("PASSKEYS")]
    public void PasskeysNavClass_ActivePageMatches_ReturnsActive(string activePage)
    {
        // Arrange
        var actionDescriptor = new ActionDescriptor { DisplayName = "/Areas/Identity/Pages/Account/Manage/Other.cshtml" };
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor);

        var mockView = new Mock<IView>(MockBehavior.Strict);
        var view = mockView.Object;

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            ["ActivePage"] = activePage
        };

        var tempData = new TempDataDictionary(actionContext.HttpContext, new Mock<ITempDataProvider>(MockBehavior.Strict).Object);
        var viewContext = new ViewContext(actionContext, view, viewData, tempData, TextWriter.Null, new HtmlHelperOptions());

        // Act
        var result = ManageNavPages.PasskeysNavClass(viewContext);

        // Assert
        Assert.Equal("active", result);
    }

    [Fact]
    public void PasskeysNavClass_NullActivePage_UsesDisplayNameFilename_ReturnsActive()
    {
        // Arrange
        var displayName = "/Areas/Identity/Pages/Account/Manage/Passkeys.cshtml";
        var actionDescriptor = new ActionDescriptor { DisplayName = displayName };
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor);

        var mockView = new Mock<IView>(MockBehavior.Strict);
        var view = mockView.Object;

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        var tempData = new TempDataDictionary(actionContext.HttpContext, new Mock<ITempDataProvider>(MockBehavior.Strict).Object);
        var viewContext = new ViewContext(actionContext, view, viewData, tempData, TextWriter.Null, new HtmlHelperOptions());

        // Act
        var result = ManageNavPages.PasskeysNavClass(viewContext);

        // Assert
        Assert.Equal("active", result);
    }

    [Fact]
    public void PasskeysNavClass_NoMatch_ReturnsNull()
    {
        // Arrange
        var actionDescriptor = new ActionDescriptor { DisplayName = "/Areas/Identity/Pages/Account/Manage/OtherPage.cshtml" };
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor);

        var mockView = new Mock<IView>(MockBehavior.Strict);
        var view = mockView.Object;

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            ["ActivePage"] = "DifferentPage"
        };

        var tempData = new TempDataDictionary(actionContext.HttpContext, new Mock<ITempDataProvider>(MockBehavior.Strict).Object);
        var viewContext = new ViewContext(actionContext, view, viewData, tempData, TextWriter.Null, new HtmlHelperOptions());

        // Act
        var result = ManageNavPages.PasskeysNavClass(viewContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PasskeysNavClass_NullViewContext_ThrowsNullReferenceException()
    {
        // Arrange
        ViewContext? viewContext = null;

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => ManageNavPages.PasskeysNavClass(viewContext!));
    }

    [Theory]
    [InlineData("Email")]
    public void Email_Property_ReturnsExpected(string expected)
    {
        // Arrange

        // Act
        var result = ManageNavPages.Email;

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.Equal(expected, result);
        Assert.Equal(expected.Length, result.Length);
    }

    [Theory]
    [InlineData("DeletePersonalData", 18)]
    public void DeletePersonalData_Property_ReturnsExpected(string expected, int expectedLength)
    {
        // Arrange

        // Act
        var actual = ManageNavPages.DeletePersonalData;

        // Assert
        Assert.NotNull(actual);
        Assert.False(string.IsNullOrWhiteSpace(actual));
        Assert.Equal(expected, actual);
        Assert.Equal(expectedLength, actual.Length);
    }

    [Fact]
    public void Passkeys_Property_ReturnsExpected()
    {
        // Arrange

        // Act
        var value = ManageNavPages.Passkeys;

        // Assert
        Assert.NotNull(value);
        Assert.False(string.IsNullOrWhiteSpace(value));
        Assert.Equal("Passkeys", value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void Passkeys_Property_IsStableAcrossAccesses(int readCount)
    {
        // Arrange

        // Act
        var first = ManageNavPages.Passkeys;
        for (var i = 0; i < readCount; i++)
        {
            var current = ManageNavPages.Passkeys;

            // Assert
            Assert.NotNull(current);
            Assert.Equal("Passkeys", current);
            Assert.Same(first, current);
        }
    }

    [Theory]
    [InlineData("DownloadPersonalData", null, "active")]
    [InlineData("downloadpersonaldata", null, "active")]
    [InlineData(null, "/Areas/Identity/Pages/Account/Manage/DownloadPersonalData.cshtml", "active")]
    [InlineData("OtherPage", "/Areas/Identity/Pages/Account/Manage/DownloadPersonalData.cshtml", null)]
    [InlineData(null, null, null)]
    public void DownloadPersonalDataNavClass_VariousActivePageAndDisplayName_ReturnsExpected(string? activePage, string? actionDisplayName, string? expected)
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var modelState = new ModelStateDictionary();
        var viewData = new ViewDataDictionary(metadataProvider, modelState);

        if (activePage != null)
        {
            viewData["ActivePage"] = activePage;
        }

        var actionDescriptor = new ActionDescriptor
        {
            DisplayName = actionDisplayName
        };

        var viewContext = new ViewContext
        {
            ViewData = viewData,
            ActionDescriptor = actionDescriptor
        };

        // Act
        var result = ManageNavPages.DownloadPersonalDataNavClass(viewContext);

        // Assert
        if (expected == null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.Equal(expected, result);
        }
    }

    [Theory]
    [InlineData("TwoFactorAuthentication")]
    [InlineData("twofactorauthentication")]
    [InlineData("TWOFACTORAUTHENTICATION")]
    public void TwoFactorAuthenticationNavClass_ActivePageMatches_ReturnsActive(string activePage)
    {
        // Arrange
        var viewContext = CreateViewContext(activePage, displayName: "/Pages/Account/Manage/SomeOtherPage.cshtml");

        // Act
        var result = ManageNavPages.TwoFactorAuthenticationNavClass(viewContext);

        // Assert
        Assert.Equal("active", result);
    }

    [Fact]
    public void TwoFactorAuthenticationNavClass_DisplayNameMatches_ReturnsActive()
    {
        // Arrange
        var viewContext = CreateViewContext(activePage: null, displayName: "/Areas/Account/Pages/Manage/TwoFactorAuthentication.cshtml");

        // Act
        var result = ManageNavPages.TwoFactorAuthenticationNavClass(viewContext);

        // Assert
        Assert.Equal("active", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("    ")]
    public void TwoFactorAuthenticationNavClass_EmptyOrWhitespaceActivePage_ReturnsNull(string activePage)
    {
        // Arrange
        var viewContext = CreateViewContext(activePage, displayName: "/Pages/Account/Manage/OtherPage.cshtml");

        // Act
        var result = ManageNavPages.TwoFactorAuthenticationNavClass(viewContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TwoFactorAuthenticationNavClass_NullDisplayNameAndNoActivePage_ReturnsNull()
    {
        // Arrange
        var viewContext = CreateViewContext(activePage: null, displayName: null);

        // Act
        var result = ManageNavPages.TwoFactorAuthenticationNavClass(viewContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TwoFactorAuthenticationNavClass_NullViewContext_ThrowsNullReferenceException()
    {
        // Arrange
        ViewContext? viewContext = null;

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => ManageNavPages.TwoFactorAuthenticationNavClass(viewContext!));
    }

    private static ViewContext CreateViewContext(string? activePage, string? displayName)
    {
        var httpContext = new DefaultHttpContext();
        var routeData = new RouteData();
        var actionDescriptor = new ActionDescriptor { DisplayName = displayName };
        var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        if (activePage is not null)
        {
            viewData["ActivePage"] = activePage;
        }

        var tempDataProviderMock = new Mock<ITempDataProvider>(MockBehavior.Strict);
        var tempData = new TempDataDictionary(httpContext, tempDataProviderMock.Object);
        var viewMock = new Mock<IView>(MockBehavior.Strict);
        var writer = new StringWriter();
        var htmlHelperOptions = new HtmlHelperOptions();

        return new ViewContext(actionContext, viewMock.Object, viewData, tempData, writer, htmlHelperOptions);
    }
}