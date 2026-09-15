namespace Identity.Tests.Unit.Pages.Account.Manage;

using Identity.Pages.Account.Manage;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ManageNavPagesTests
{
    private static readonly string WhitespaceActivePage = TestValues.NewWhitespaceValue();

    public static TheoryData<object?, string?, string?> DeletePersonalDataCases()
    {
        var page = ManageNavPages.DeletePersonalData;
        var unrelatedPath = PathEndingIn(TestValues.NewDifferentPageName());
        var matchingPath = PathEndingIn(page);
        return new TheoryData<object?, string?, string?>
        {
            { page, unrelatedPath, ManageNavPages.ActiveNavClass },
            { page.ToLowerInvariant(), unrelatedPath, ManageNavPages.ActiveNavClass },
            { TestValues.NewDifferentPageName(), matchingPath, null },
            { null, matchingPath, ManageNavPages.ActiveNavClass },
            { TestValues.NewEntityId(), matchingPath, ManageNavPages.ActiveNavClass },
            { null, null, null },
        };
    }

    public static TheoryData<object?, string?, string, string?> PageNavTestData()
    {
        var page = TestValues.NewPageName();
        var differentPage = TestValues.NewDifferentPageName();
        var punctuatedPage = TestValues.NewPunctuatedPageName();
        var overlongPage = TestValues.NewOverlongPageName();
        return new TheoryData<object?, string?, string, string?>
        {
            { page, PathEndingIn(page), page, ManageNavPages.ActiveNavClass },
            { page.ToUpperInvariant(), PathEndingIn(page), page, ManageNavPages.ActiveNavClass },
            { string.Empty, PathEndingIn(differentPage), string.Empty, ManageNavPages.ActiveNavClass },
            { null, PathEndingIn(page), page, ManageNavPages.ActiveNavClass },
            { TestValues.NewEntityId(), PathEndingIn(page), page, ManageNavPages.ActiveNavClass },
            { differentPage, PathEndingIn(page), page, null },
            { null, null, page, null },
            { null, PathEndingIn(punctuatedPage), punctuatedPage, ManageNavPages.ActiveNavClass },
            { WhitespaceActivePage, PathEndingIn(differentPage), WhitespaceActivePage, ManageNavPages.ActiveNavClass },
            { overlongPage, PathEndingIn(differentPage), overlongPage, ManageNavPages.ActiveNavClass },
            { null, FileNameFor(page), page, ManageNavPages.ActiveNavClass },
        };
    }

    public static TheoryData<object?, string?, string?> EmailNavClassCases()
    {
        var page = ManageNavPages.Email;
        var matchingPath = PathEndingIn(page);
        return new TheoryData<object?, string?, string?>
        {
            { page, null, ManageNavPages.ActiveNavClass },
            { page.ToLowerInvariant(), null, ManageNavPages.ActiveNavClass },
            { TestValues.NewDifferentPageName(), null, null },
            { null, matchingPath, ManageNavPages.ActiveNavClass },
            { null, PathEndingIn(TestValues.NewDifferentPageName()), null },
            { TestValues.NewEntityId(), matchingPath, ManageNavPages.ActiveNavClass },
            { null, null, null },
            { null, PathEndingIn(page.ToUpperInvariant()), ManageNavPages.ActiveNavClass },
            { WhitespaceActivePage, matchingPath, null },
        };
    }

    public static TheoryData<string?, string?, string?> PageCases()
    {
        var page = ManageNavPages.ChangePassword;
        return new TheoryData<string?, string?, string?>
        {
            { page, null, ManageNavPages.ActiveNavClass },
            { page.ToLowerInvariant(), null, ManageNavPages.ActiveNavClass },
            { null, PathEndingIn(page), ManageNavPages.ActiveNavClass },
            { null, PathEndingIn(TestValues.NewDifferentPageName()), null },
            { null, null, null },
        };
    }

    public static TheoryData<string?, string?, string?> GetPersonalDataNavCases()
    {
        var page = ManageNavPages.PersonalData;
        return new TheoryData<string?, string?, string?>
        {
            { page, null, ManageNavPages.ActiveNavClass },
            { page.ToLowerInvariant(), null, ManageNavPages.ActiveNavClass },
            { null, PathEndingIn(page), ManageNavPages.ActiveNavClass },
            { null, WindowsPathEndingIn(page), ManageNavPages.ActiveNavClass },
            { null, null, null },
            { string.Empty, PathEndingIn(page), null },
            { WhitespaceActivePage, null, null },
            { page + WhitespaceActivePage, null, null },
            { TestValues.NewOverlongPageName(), null, null },
            { page + TestValues.NewDifferentPageName(), null, null },
            { null, PathEndingIn(TestValues.NewPathSegment() + '.' + page), null },
        };
    }

    public static TheoryData<string> IndexActivePageMatches() => new()
    {
        ManageNavPages.Index,
        ManageNavPages.Index.ToLowerInvariant(),
        ManageNavPages.Index.ToUpperInvariant(),
    };

    public static TheoryData<string?> IndexDisplayNameMatches() => new()
    {
        FileNameFor(ManageNavPages.Index),
        FileNameFor(ManageNavPages.Index.ToLowerInvariant()),
        PathEndingIn(ManageNavPages.Index),
        ManageNavPages.Index,
    };

    public static TheoryData<string?, string?> IndexNoMatchCases()
    {
        var differentPage = TestValues.NewDifferentPageName();
        return new TheoryData<string?, string?>
        {
            { differentPage, PathEndingIn(differentPage) },
            { null, null },
            { string.Empty, string.Empty },
            { WhitespaceActivePage, WhitespaceActivePage },
        };
    }

    public static TheoryData<string?, string?, string?> ExternalLoginsNavCases()
    {
        var page = ManageNavPages.ExternalLogins;
        return new TheoryData<string?, string?, string?>
        {
            { page, null, ManageNavPages.ActiveNavClass },
            { page.ToLowerInvariant(), null, ManageNavPages.ActiveNavClass },
            { null, PathEndingIn(page), ManageNavPages.ActiveNavClass },
            { null, PathEndingIn(TestValues.NewDifferentPageName()), null },
            { string.Empty, PathEndingIn(page), null },
            { WhitespaceActivePage + page + WhitespaceActivePage, null, null },
        };
    }

    public static TheoryData<string?, string?, string?> DownloadPersonalDataNavCases()
    {
        var page = ManageNavPages.DownloadPersonalData;
        return new TheoryData<string?, string?, string?>
        {
            { page, null, ManageNavPages.ActiveNavClass },
            { page.ToLowerInvariant(), null, ManageNavPages.ActiveNavClass },
            { null, PathEndingIn(page), ManageNavPages.ActiveNavClass },
            { TestValues.NewDifferentPageName(), PathEndingIn(page), null },
            { null, null, null },
        };
    }

    public static TheoryData<string> PasskeysActivePageMatches() => new()
    {
        ManageNavPages.Passkeys,
        ManageNavPages.Passkeys.ToLowerInvariant(),
        ManageNavPages.Passkeys.ToUpperInvariant(),
    };

    public static TheoryData<string> TwoFactorAuthenticationActivePageMatches() => new()
    {
        ManageNavPages.TwoFactorAuthentication,
        ManageNavPages.TwoFactorAuthentication.ToLowerInvariant(),
        ManageNavPages.TwoFactorAuthentication.ToUpperInvariant(),
    };

    public static TheoryData<string> BlankActivePages() => new()
    {
        string.Empty,
        WhitespaceActivePage,
    };

    [Fact]
    public void Index_Property_ReturnsExpected()
    {
        // Act
        var page = ManageNavPages.Index;

        // Assert
        Assert.Equal("Index", page);
    }

    [Fact]
    public void ExternalLogins_Property_ReturnsExpected()
    {
        // Act
        var page = ManageNavPages.ExternalLogins;

        // Assert
        Assert.Equal("ExternalLogins", page);
    }

    [Theory]
    [MemberData(nameof(IndexActivePageMatches))]
    public void IndexNavClass_ActivePageMatches_ReturnsActive(string activePage)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var actionDescriptor = new ActionDescriptor { DisplayName = PathEndingIn(TestValues.NewDifferentPageName()) };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);

        var metadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary())
        {
            [ManageNavPages.ActivePageViewDataKey] = activePage
        };

        var mockView = new Mock<IView>(MockBehavior.Strict);
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        var viewContext = new ViewContext(actionContext, mockView.Object, viewData, tempData, TextWriter.Null, new HtmlHelperOptions());

        // Act
        var result = ManageNavPages.IndexNavClass(viewContext);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Theory]
    [MemberData(nameof(IndexDisplayNameMatches))]
    public void IndexNavClass_NullActivePage_UsesDisplayNameFilename_ReturnsActive(string? displayName)
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
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Theory]
    [MemberData(nameof(IndexNoMatchCases))]
    public void IndexNavClass_NoMatch_ReturnsNull(string? activePage, string? displayName)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var actionDescriptor = new ActionDescriptor { DisplayName = displayName };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);

        var metadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary())
        {
            [ManageNavPages.ActivePageViewDataKey] = activePage
        };

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
        var actionDescriptor = new ActionDescriptor { DisplayName = FileNameFor(ManageNavPages.Index) };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);

        var metadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary())
        {
            [ManageNavPages.ActivePageViewDataKey] = TestValues.NewEntityId()
        };

        var mockView = new Mock<IView>(MockBehavior.Strict);
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        var viewContext = new ViewContext(actionContext, mockView.Object, viewData, tempData, TextWriter.Null, new HtmlHelperOptions());

        // Act
        var result = ManageNavPages.IndexNavClass(viewContext);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

#pragma warning disable xUnit1045
    [Theory]
    [MemberData(nameof(DeletePersonalDataCases))]
    public void DeletePersonalDataNavClass_VariousViewContexts_ReturnsExpected(object? activePageValue, string? displayName, string? expected)
    {
        // Arrange
        var viewContext = new ViewContext
        {
            ActionDescriptor = new ActionDescriptor { DisplayName = displayName },
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                [ManageNavPages.ActivePageViewDataKey] = activePageValue
            }
        };

        // Act
        var result = ManageNavPages.DeletePersonalDataNavClass(viewContext);

        // Assert
        Assert.Equal(expected, result);
    }
#pragma warning restore xUnit1045

    [Fact]
    public void DeletePersonalDataNavClass_NullViewContext_ThrowsArgumentNullException()
    {
        // Arrange
        ViewContext? viewContext = null;

        // Act
        var exception = Record.Exception(() => ManageNavPages.DeletePersonalDataNavClass(viewContext));

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

#pragma warning disable xUnit1045
    [Theory]
    [MemberData(nameof(PageNavTestData))]
    public void PageNavClass_VariousInputs_ReturnsExpected(object? activePage, string? displayName, string page, string? expected)
    {
        // Arrange
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        viewData[ManageNavPages.ActivePageViewDataKey] = activePage;
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

    [Fact]
    public void DownloadPersonalData_Property_ReturnsExpected()
    {
        // Act
        var page = ManageNavPages.DownloadPersonalData;

        // Assert
        Assert.Equal("DownloadPersonalData", page);
    }

    [Fact]
    public void PersonalData_Property_ReturnsExpected()
    {
        // Act
        var page = ManageNavPages.PersonalData;

        // Assert
        Assert.Equal("PersonalData", page);
    }

    [Fact]
    public void PersonalData_Property_IsStableAcrossAccesses()
    {
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
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            [ManageNavPages.ActivePageViewDataKey] = activePageValue
        };

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
        Assert.Equal(expected, result);
    }
#pragma warning restore xUnit1045

    [Theory]
    [MemberData(nameof(ExternalLoginsNavCases))]
    public void ExternalLoginsNavClass_VariousActivePageAndDisplayName_ReturnsExpected(string? activePage, string? displayName, string? expected)
    {
        // Arrange
        var viewContext = CreateViewContext(activePage, displayName);

        // Act
        var result = ManageNavPages.ExternalLoginsNavClass(viewContext);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExternalLoginsNavClass_NullViewContext_ThrowsArgumentNullException()
    {
        // Arrange
        ViewContext? viewContext = null;

        // Act
        var exception = Record.Exception(() => ManageNavPages.ExternalLoginsNavClass(viewContext));

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public void ChangePassword_Property_ReturnsExpected()
    {
        // Act
        var page = ManageNavPages.ChangePassword;

        // Assert
        Assert.Equal("ChangePassword", page);
    }

    [Fact]
    public void ChangePassword_Property_IsStableAcrossAccesses()
    {
        // Act
        var first = ManageNavPages.ChangePassword;
        var second = ManageNavPages.ChangePassword;

        // Assert
        Assert.Equal(first, second);
        Assert.NotNull(first);
        Assert.NotEmpty(first);
    }

    [Fact]
    public void TwoFactorAuthentication_Property_ReturnsExpected()
    {
        // Act
        var page = ManageNavPages.TwoFactorAuthentication;

        // Assert
        Assert.Equal("TwoFactorAuthentication", page);
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

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            [ManageNavPages.ActivePageViewDataKey] = activePage
        };

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
            [ManageNavPages.ActivePageViewDataKey] = WhitespaceActivePage
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
    [MemberData(nameof(PasskeysActivePageMatches))]
    public void PasskeysNavClass_ActivePageMatches_ReturnsActive(string activePage)
    {
        // Arrange
        var actionDescriptor = new ActionDescriptor { DisplayName = PathEndingIn(TestValues.NewDifferentPageName()) };
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor);

        var mockView = new Mock<IView>(MockBehavior.Strict);
        var view = mockView.Object;

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            [ManageNavPages.ActivePageViewDataKey] = activePage
        };

        var tempData = new TempDataDictionary(actionContext.HttpContext, new Mock<ITempDataProvider>(MockBehavior.Strict).Object);
        var viewContext = new ViewContext(actionContext, view, viewData, tempData, TextWriter.Null, new HtmlHelperOptions());

        // Act
        var result = ManageNavPages.PasskeysNavClass(viewContext);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PasskeysNavClass_NullActivePage_UsesDisplayNameFilename_ReturnsActive()
    {
        // Arrange
        var displayName = PathEndingIn(ManageNavPages.Passkeys);
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
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PasskeysNavClass_NoMatch_ReturnsNull()
    {
        // Arrange
        var differentPage = TestValues.NewDifferentPageName();
        var actionDescriptor = new ActionDescriptor { DisplayName = PathEndingIn(differentPage) };
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor);

        var mockView = new Mock<IView>(MockBehavior.Strict);
        var view = mockView.Object;

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            [ManageNavPages.ActivePageViewDataKey] = differentPage
        };

        var tempData = new TempDataDictionary(actionContext.HttpContext, new Mock<ITempDataProvider>(MockBehavior.Strict).Object);
        var viewContext = new ViewContext(actionContext, view, viewData, tempData, TextWriter.Null, new HtmlHelperOptions());

        // Act
        var result = ManageNavPages.PasskeysNavClass(viewContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PasskeysNavClass_NullViewContext_ThrowsArgumentNullException()
    {
        // Arrange
        ViewContext? viewContext = null;

        // Act
        var exception = Record.Exception(() => ManageNavPages.PasskeysNavClass(viewContext));

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public void Email_Property_ReturnsExpected()
    {
        // Act
        var page = ManageNavPages.Email;

        // Assert
        Assert.Equal("Email", page);
    }

    [Fact]
    public void DeletePersonalData_Property_ReturnsExpected()
    {
        // Act
        var page = ManageNavPages.DeletePersonalData;

        // Assert
        Assert.Equal("DeletePersonalData", page);
    }

    [Fact]
    public void Passkeys_Property_ReturnsExpected()
    {
        // Act
        var page = ManageNavPages.Passkeys;

        // Assert
        Assert.Equal("Passkeys", page);
    }

    [Fact]
    public void Passkeys_Property_IsStableAcrossAccesses()
    {
        // Act
        var first = ManageNavPages.Passkeys;
        var second = ManageNavPages.Passkeys;
        var third = ManageNavPages.Passkeys;

        // Assert
        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.Same(first, third);
    }

    [Theory]
    [MemberData(nameof(DownloadPersonalDataNavCases))]
    public void DownloadPersonalDataNavClass_VariousActivePageAndDisplayName_ReturnsExpected(string? activePage, string? actionDisplayName, string? expected)
    {
        // Arrange
        var metadataProvider = new EmptyModelMetadataProvider();
        var modelState = new ModelStateDictionary();
        var viewData = new ViewDataDictionary(metadataProvider, modelState)
        {
            [ManageNavPages.ActivePageViewDataKey] = activePage
        };

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
        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(TwoFactorAuthenticationActivePageMatches))]
    public void TwoFactorAuthenticationNavClass_ActivePageMatches_ReturnsActive(string activePage)
    {
        // Arrange
        var viewContext = CreateViewContext(activePage, displayName: PathEndingIn(TestValues.NewDifferentPageName()));

        // Act
        var result = ManageNavPages.TwoFactorAuthenticationNavClass(viewContext);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void TwoFactorAuthenticationNavClass_DisplayNameMatches_ReturnsActive()
    {
        // Arrange
        var viewContext = CreateViewContext(activePage: null, displayName: PathEndingIn(ManageNavPages.TwoFactorAuthentication));

        // Act
        var result = ManageNavPages.TwoFactorAuthenticationNavClass(viewContext);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Theory]
    [MemberData(nameof(BlankActivePages))]
    public void TwoFactorAuthenticationNavClass_EmptyOrWhitespaceActivePage_ReturnsNull(string activePage)
    {
        // Arrange
        var viewContext = CreateViewContext(activePage, displayName: PathEndingIn(TestValues.NewDifferentPageName()));

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
    public void TwoFactorAuthenticationNavClass_NullViewContext_ThrowsArgumentNullException()
    {
        // Arrange
        ViewContext? viewContext = null;

        // Act
        var exception = Record.Exception(() => ManageNavPages.TwoFactorAuthenticationNavClass(viewContext));

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
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
            viewData[ManageNavPages.ActivePageViewDataKey] = activePage;
        }

        var tempDataProviderMock = new Mock<ITempDataProvider>(MockBehavior.Strict);
        var tempData = new TempDataDictionary(httpContext, tempDataProviderMock.Object);
        var viewMock = new Mock<IView>(MockBehavior.Strict);
        var writer = new StringWriter();
        var htmlHelperOptions = new HtmlHelperOptions();

        return new ViewContext(actionContext, viewMock.Object, viewData, tempData, writer, htmlHelperOptions);
    }

    private static string FileNameFor(string page) => page + RazorViewEngine.ViewExtension;

    private static string PathEndingIn(string page) => JoinPath('/', page);

    private static string WindowsPathEndingIn(string page) => JoinPath('\\', page);

    private static string JoinPath(char separator, string page) =>
        string.Join(separator, TestValues.NewPathSegment(), TestValues.NewPathSegment(), FileNameFor(page));
}