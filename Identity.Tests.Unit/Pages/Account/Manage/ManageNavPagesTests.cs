namespace Identity.Tests.Unit.Pages.Account.Manage;

using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
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
    public static TheoryData<string> NonStringActivePagePages() => new()
    {
        ManageNavPages.DeletePersonalData,
        ManageNavPages.Email,
    };

    public static TheoryData<string> EmailActivePageMatches() => new()
    {
        ManageNavPages.Email,
        ManageNavPages.Email.ToLowerInvariant(),
    };

    public static TheoryData<string> ChangePasswordActivePageMatches() => new()
    {
        ManageNavPages.ChangePassword,
        ManageNavPages.ChangePassword.ToLowerInvariant(),
    };

    public static TheoryData<string> PersonalDataActivePageMatches() => new()
    {
        ManageNavPages.PersonalData,
        ManageNavPages.PersonalData.ToLowerInvariant(),
    };

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
        ManageNavPages.Index,
    };

    public static TheoryData<string> ExternalLoginsActivePageMatches() => new()
    {
        ManageNavPages.ExternalLogins,
        ManageNavPages.ExternalLogins.ToLowerInvariant(),
    };

    public static TheoryData<string> DownloadPersonalDataActivePageMatches() => new()
    {
        ManageNavPages.DownloadPersonalData,
        ManageNavPages.DownloadPersonalData.ToLowerInvariant(),
    };

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
    public void PageNavClass_Index_ActivePageMatches_ReturnsActive(string activePage)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var actionDescriptor = new ActionDescriptor { DisplayName = PathEndingIn(Generated.NewDifferentPageName()) };
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
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Index);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Theory]
    [MemberData(nameof(IndexDisplayNameMatches))]
    public void PageNavClass_Index_NullActivePage_UsesDisplayNameFilename_ReturnsActive(string? displayName)
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
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Index);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_Index_NullActivePageAndPathDisplayName_ReturnsActive()
    {
        // Arrange
        var indexDisplayName = PathEndingIn(ManageNavPages.Index);
        var viewContext = CreateViewContext(activePage: null, displayName: indexDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Index);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_Index_NullActivePageAndNullDisplayName_ReturnsNull()
    {
        // Arrange
        var viewContext = CreateViewContext(activePage: null, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Index);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_Index_BlankActivePageAndBlankDisplayName_ReturnsNull()
    {
        // Arrange
        var blankActivePage = Generated.NewBlank();
        var blankDisplayName = Generated.NewBlank();
        var viewContext = CreateViewContext(blankActivePage, blankDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Index);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_Index_DifferentActivePageAndDisplayName_ReturnsNull()
    {
        // Arrange
        var differentPage = Generated.NewDifferentPageName();
        var differentDisplayName = PathEndingIn(differentPage);
        var viewContext = CreateViewContext(differentPage, differentDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Index);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_Index_WhitespaceActivePageAndDisplayName_ReturnsNull()
    {
        // Arrange
        var whitespacePageName = Generated.NewWhitespaceValue();
        var viewContext = CreateViewContext(whitespacePageName, whitespacePageName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Index);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_Index_NonStringActivePage_FallsBackToDisplayName()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var actionDescriptor = new ActionDescriptor { DisplayName = FileNameFor(ManageNavPages.Index) };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);

        var metadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary())
        {
            [ManageNavPages.ActivePageViewDataKey] = Generated.NewEntityId()
        };

        var mockView = new Mock<IView>(MockBehavior.Strict);
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        var viewContext = new ViewContext(actionContext, mockView.Object, viewData, tempData, TextWriter.Null, new HtmlHelperOptions());

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Index);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Theory]
    [MemberData(nameof(NonStringActivePagePages))]
    public void PageNavClass_NonStringActivePage_FallsBackToTheDisplayName(string page)
    {
        // Arrange
        var viewContext = new ViewContext
        {
            ActionDescriptor = new ActionDescriptor { DisplayName = PathEndingIn(page) },
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                [ManageNavPages.ActivePageViewDataKey] = Generated.NewEntityId()
            }
        };

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, page);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_NonStringActivePageForAnUnlistedPage_FallsBackToTheDisplayName()
    {
        // Arrange
        var unlistedPage = Generated.NewPageName();
        var unlistedPageDisplayName = PathEndingIn(unlistedPage);
        var nonStringActivePage = Generated.NewEntityId();
        var viewContext = new ViewContext
        {
            ActionDescriptor = new ActionDescriptor { DisplayName = unlistedPageDisplayName },
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                [ManageNavPages.ActivePageViewDataKey] = nonStringActivePage
            }
        };

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, unlistedPage);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_DeletePersonalData_ActivePageMatches_ReturnsActive()
    {
        // Arrange
        var unrelatedPage = Generated.NewDifferentPageName();
        var unrelatedDisplayName = PathEndingIn(unrelatedPage);
        var viewContext = CreateViewContext(ManageNavPages.DeletePersonalData, unrelatedDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.DeletePersonalData);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_DeletePersonalData_LowercaseActivePage_ReturnsActive()
    {
        // Arrange
        var unrelatedPage = Generated.NewDifferentPageName();
        var unrelatedDisplayName = PathEndingIn(unrelatedPage);
        var lowercaseActivePage = ManageNavPages.DeletePersonalData.ToLowerInvariant();
        var viewContext = CreateViewContext(lowercaseActivePage, unrelatedDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.DeletePersonalData);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_DeletePersonalData_DifferentActivePageAndMatchingDisplayName_ReturnsNull()
    {
        // Arrange
        var differentActivePage = Generated.NewDifferentPageName();
        var matchingDisplayName = PathEndingIn(ManageNavPages.DeletePersonalData);
        var viewContext = CreateViewContext(differentActivePage, matchingDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.DeletePersonalData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_DeletePersonalData_NullActivePageAndMatchingDisplayName_ReturnsActive()
    {
        // Arrange
        var matchingDisplayName = PathEndingIn(ManageNavPages.DeletePersonalData);
        var viewContext = CreateViewContext(activePage: null, displayName: matchingDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.DeletePersonalData);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_DeletePersonalData_NullActivePageAndNullDisplayName_ReturnsNull()
    {
        // Arrange
        var viewContext = CreateViewContext(activePage: null, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.DeletePersonalData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_DeletePersonalData_NullViewContext_ThrowsArgumentNullException()
    {
        // Arrange
        ViewContext? viewContext = null;

        // Act
        var exception = Record.Exception(() => ManageNavPages.PageNavClass(viewContext, ManageNavPages.DeletePersonalData));

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public void PageNavClass_ActivePageAndDisplayNameMatchThePage_ReturnsActive()
    {
        // Arrange
        var page = Generated.NewPageName();
        var matchingDisplayName = PathEndingIn(page);
        var viewContext = CreateViewContext(page, matchingDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, page);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_UppercaseActivePage_ReturnsActive()
    {
        // Arrange
        var page = Generated.NewPageName();
        var matchingDisplayName = PathEndingIn(page);
        var uppercaseActivePage = page.ToUpperInvariant();
        var viewContext = CreateViewContext(uppercaseActivePage, matchingDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, page);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_BlankActivePageForTheSameBlankPage_ReturnsActive()
    {
        // Arrange
        var differentPage = Generated.NewDifferentPageName();
        var differentDisplayName = PathEndingIn(differentPage);
        var blankPage = Generated.NewBlank();
        var viewContext = CreateViewContext(blankPage, differentDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, blankPage);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_NullActivePageAndMatchingDisplayName_ReturnsActive()
    {
        // Arrange
        var page = Generated.NewPageName();
        var matchingDisplayName = PathEndingIn(page);
        var viewContext = CreateViewContext(activePage: null, displayName: matchingDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, page);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_DifferentActivePageAndMatchingDisplayName_ReturnsNull()
    {
        // Arrange
        var page = Generated.NewPageName();
        var matchingDisplayName = PathEndingIn(page);
        var differentActivePage = Generated.NewDifferentPageName();
        var viewContext = CreateViewContext(differentActivePage, matchingDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, page);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_NullActivePageAndNullDisplayName_ReturnsNull()
    {
        // Arrange
        var page = Generated.NewPageName();
        var viewContext = CreateViewContext(activePage: null, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, page);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_NullActivePageAndPunctuatedDisplayName_ReturnsActive()
    {
        // Arrange
        var punctuatedPage = Generated.NewPunctuatedPageName();
        var punctuatedDisplayName = PathEndingIn(punctuatedPage);
        var viewContext = CreateViewContext(activePage: null, displayName: punctuatedDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, punctuatedPage);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_WhitespaceActivePageForAWhitespacePage_ReturnsActive()
    {
        // Arrange
        var whitespacePage = Generated.NewWhitespaceValue();
        var differentPage = Generated.NewDifferentPageName();
        var differentDisplayName = PathEndingIn(differentPage);
        var viewContext = CreateViewContext(whitespacePage, differentDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, whitespacePage);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_OverlongActivePageForAnOverlongPage_ReturnsActive()
    {
        // Arrange
        var overlongPage = Generated.NewOverlongPageName();
        var differentPage = Generated.NewDifferentPageName();
        var differentDisplayName = PathEndingIn(differentPage);
        var viewContext = CreateViewContext(overlongPage, differentDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, overlongPage);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_NullActivePageAndBareFileNameDisplayName_ReturnsActive()
    {
        // Arrange
        var page = Generated.NewPageName();
        var fileNameDisplayName = FileNameFor(page);
        var viewContext = CreateViewContext(activePage: null, displayName: fileNameDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, page);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

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

    [Theory]
    [MemberData(nameof(EmailActivePageMatches))]
    public void PageNavClass_Email_ActivePageMatches_ReturnsActive(string activePage)
    {
        // Arrange
        var viewContext = CreateViewContext(activePage, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Email);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_Email_DifferentActivePage_ReturnsNull()
    {
        // Arrange
        var differentActivePage = Generated.NewDifferentPageName();
        var viewContext = CreateViewContext(differentActivePage, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Email);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_Email_NullActivePageAndMatchingDisplayName_ReturnsActive()
    {
        // Arrange
        var matchingDisplayName = PathEndingIn(ManageNavPages.Email);
        var viewContext = CreateViewContext(activePage: null, displayName: matchingDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Email);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_Email_NullActivePageAndDifferentDisplayName_ReturnsNull()
    {
        // Arrange
        var differentPage = Generated.NewDifferentPageName();
        var differentDisplayName = PathEndingIn(differentPage);
        var viewContext = CreateViewContext(activePage: null, displayName: differentDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Email);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_Email_NullActivePageAndNullDisplayName_ReturnsNull()
    {
        // Arrange
        var viewContext = CreateViewContext(activePage: null, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Email);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_Email_NullActivePageAndUppercaseDisplayName_ReturnsActive()
    {
        // Arrange
        var uppercaseDisplayName = PathEndingIn(ManageNavPages.Email.ToUpperInvariant());
        var viewContext = CreateViewContext(activePage: null, displayName: uppercaseDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Email);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_Email_WhitespaceActivePageAndMatchingDisplayName_ReturnsNull()
    {
        // Arrange
        var whitespaceActivePage = Generated.NewWhitespaceValue();
        var matchingDisplayName = PathEndingIn(ManageNavPages.Email);
        var viewContext = CreateViewContext(whitespaceActivePage, matchingDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Email);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [MemberData(nameof(ExternalLoginsActivePageMatches))]
    public void PageNavClass_ExternalLogins_ActivePageMatches_ReturnsActive(string activePage)
    {
        // Arrange
        var viewContext = CreateViewContext(activePage, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.ExternalLogins);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_ExternalLogins_NullActivePageAndMatchingDisplayName_ReturnsActive()
    {
        // Arrange
        var matchingDisplayName = PathEndingIn(ManageNavPages.ExternalLogins);
        var viewContext = CreateViewContext(activePage: null, displayName: matchingDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.ExternalLogins);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_ExternalLogins_NullActivePageAndDifferentDisplayName_ReturnsNull()
    {
        // Arrange
        var differentPage = Generated.NewDifferentPageName();
        var differentDisplayName = PathEndingIn(differentPage);
        var viewContext = CreateViewContext(activePage: null, displayName: differentDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.ExternalLogins);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_ExternalLogins_BlankActivePageAndMatchingDisplayName_ReturnsNull()
    {
        // Arrange
        var matchingDisplayName = PathEndingIn(ManageNavPages.ExternalLogins);
        var viewContext = CreateViewContext(Generated.NewBlank(), matchingDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.ExternalLogins);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_ExternalLogins_WhitespacePaddedActivePage_ReturnsNull()
    {
        // Arrange
        var padding = Generated.NewWhitespaceValue();
        var paddedActivePage = padding + ManageNavPages.ExternalLogins + padding;
        var viewContext = CreateViewContext(paddedActivePage, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.ExternalLogins);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_ExternalLogins_NullViewContext_ThrowsArgumentNullException()
    {
        // Arrange
        ViewContext? viewContext = null;

        // Act
        var exception = Record.Exception(() => ManageNavPages.PageNavClass(viewContext, ManageNavPages.ExternalLogins));

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
    [MemberData(nameof(ChangePasswordActivePageMatches))]
    public void PageNavClass_ChangePassword_ActivePageMatches_ReturnsActive(string activePage)
    {
        // Arrange
        var viewContext = CreateViewContext(activePage, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.ChangePassword);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_ChangePassword_NullActivePageAndMatchingDisplayName_ReturnsActive()
    {
        // Arrange
        var matchingDisplayName = PathEndingIn(ManageNavPages.ChangePassword);
        var viewContext = CreateViewContext(activePage: null, displayName: matchingDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.ChangePassword);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_ChangePassword_NullActivePageAndDifferentDisplayName_ReturnsNull()
    {
        // Arrange
        var differentPage = Generated.NewDifferentPageName();
        var differentDisplayName = PathEndingIn(differentPage);
        var viewContext = CreateViewContext(activePage: null, displayName: differentDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.ChangePassword);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_ChangePassword_NullActivePageAndNullDisplayName_ReturnsNull()
    {
        // Arrange
        var viewContext = CreateViewContext(activePage: null, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.ChangePassword);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_ChangePassword_WhitespaceActivePage_ReturnsNull()
    {
        // Arrange
        var whitespaceActivePage = Generated.NewWhitespaceValue();
        var actionDescriptor = new ActionDescriptor { DisplayName = null };
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            [ManageNavPages.ActivePageViewDataKey] = whitespaceActivePage
        };

        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        var view = new Mock<IView>(MockBehavior.Strict).Object;
        var viewContext = new ViewContext(actionContext, view, viewData, tempData, new StringWriter(), new HtmlHelperOptions());

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.ChangePassword);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [MemberData(nameof(PersonalDataActivePageMatches))]
    public void PageNavClass_PersonalData_ActivePageMatches_ReturnsActive(string activePage)
    {
        // Arrange
        var viewContext = CreateViewContext(activePage, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.PersonalData);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_PersonalData_NullActivePageAndMatchingDisplayName_ReturnsActive()
    {
        // Arrange
        var matchingDisplayName = PathEndingIn(ManageNavPages.PersonalData);
        var viewContext = CreateViewContext(activePage: null, displayName: matchingDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.PersonalData);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_PersonalData_NullActivePageAndWindowsPathDisplayName_ReturnsActive()
    {
        // Arrange
        var windowsPathDisplayName = WindowsPathEndingIn(ManageNavPages.PersonalData);
        var viewContext = CreateViewContext(activePage: null, displayName: windowsPathDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.PersonalData);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_PersonalData_NullActivePageAndNullDisplayName_ReturnsNull()
    {
        // Arrange
        var viewContext = CreateViewContext(activePage: null, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.PersonalData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_PersonalData_BlankActivePageAndMatchingDisplayName_ReturnsNull()
    {
        // Arrange
        var matchingDisplayName = PathEndingIn(ManageNavPages.PersonalData);
        var viewContext = CreateViewContext(Generated.NewBlank(), matchingDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.PersonalData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_PersonalData_WhitespaceActivePage_ReturnsNull()
    {
        // Arrange
        var whitespaceActivePage = Generated.NewWhitespaceValue();
        var viewContext = CreateViewContext(whitespaceActivePage, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.PersonalData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_PersonalData_ActivePageWithTrailingWhitespace_ReturnsNull()
    {
        // Arrange
        var trailingWhitespace = Generated.NewWhitespaceValue();
        var paddedActivePage = ManageNavPages.PersonalData + trailingWhitespace;
        var viewContext = CreateViewContext(paddedActivePage, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.PersonalData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_PersonalData_OverlongActivePage_ReturnsNull()
    {
        // Arrange
        var overlongActivePage = Generated.NewOverlongPageName();
        var viewContext = CreateViewContext(overlongActivePage, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.PersonalData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_PersonalData_ActivePageWithSuffix_ReturnsNull()
    {
        // Arrange
        var suffix = Generated.NewDifferentPageName();
        var suffixedActivePage = ManageNavPages.PersonalData + suffix;
        var viewContext = CreateViewContext(suffixedActivePage, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.PersonalData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_PersonalData_NullActivePageAndDottedPrefixDisplayName_ReturnsNull()
    {
        // Arrange
        var prefixSegment = Generated.NewPathSegment();
        var dottedPage = prefixSegment + '.' + ManageNavPages.PersonalData;
        var dottedDisplayName = PathEndingIn(dottedPage);
        var viewContext = CreateViewContext(activePage: null, displayName: dottedDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.PersonalData);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [MemberData(nameof(PasskeysActivePageMatches))]
    public void PageNavClass_Passkeys_ActivePageMatches_ReturnsActive(string activePage)
    {
        // Arrange
        var actionDescriptor = new ActionDescriptor { DisplayName = PathEndingIn(Generated.NewDifferentPageName()) };
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
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Passkeys);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_Passkeys_NullActivePage_UsesDisplayNameFilename_ReturnsActive()
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
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Passkeys);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_Passkeys_NoMatch_ReturnsNull()
    {
        // Arrange
        var differentPage = Generated.NewDifferentPageName();
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
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.Passkeys);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_Passkeys_NullViewContext_ThrowsArgumentNullException()
    {
        // Arrange
        ViewContext? viewContext = null;

        // Act
        var exception = Record.Exception(() => ManageNavPages.PageNavClass(viewContext, ManageNavPages.Passkeys));

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
    [MemberData(nameof(DownloadPersonalDataActivePageMatches))]
    public void PageNavClass_DownloadPersonalData_ActivePageMatches_ReturnsActive(string activePage)
    {
        // Arrange
        var viewContext = CreateViewContext(activePage, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.DownloadPersonalData);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_DownloadPersonalData_NullActivePageAndMatchingDisplayName_ReturnsActive()
    {
        // Arrange
        var matchingDisplayName = PathEndingIn(ManageNavPages.DownloadPersonalData);
        var viewContext = CreateViewContext(activePage: null, displayName: matchingDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.DownloadPersonalData);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_DownloadPersonalData_DifferentActivePageAndMatchingDisplayName_ReturnsNull()
    {
        // Arrange
        var differentActivePage = Generated.NewDifferentPageName();
        var matchingDisplayName = PathEndingIn(ManageNavPages.DownloadPersonalData);
        var viewContext = CreateViewContext(differentActivePage, matchingDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.DownloadPersonalData);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_DownloadPersonalData_NullActivePageAndNullDisplayName_ReturnsNull()
    {
        // Arrange
        var viewContext = CreateViewContext(activePage: null, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.DownloadPersonalData);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [MemberData(nameof(TwoFactorAuthenticationActivePageMatches))]
    public void PageNavClass_TwoFactorAuthentication_ActivePageMatches_ReturnsActive(string activePage)
    {
        // Arrange
        var viewContext = CreateViewContext(activePage, displayName: PathEndingIn(Generated.NewDifferentPageName()));

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.TwoFactorAuthentication);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_TwoFactorAuthentication_DisplayNameMatches_ReturnsActive()
    {
        // Arrange
        var viewContext = CreateViewContext(activePage: null, displayName: PathEndingIn(ManageNavPages.TwoFactorAuthentication));

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.TwoFactorAuthentication);

        // Assert
        Assert.Equal(ManageNavPages.ActiveNavClass, result);
    }

    [Fact]
    public void PageNavClass_TwoFactorAuthentication_BlankActivePage_ReturnsNull()
    {
        // Arrange
        var differentPage = Generated.NewDifferentPageName();
        var differentDisplayName = PathEndingIn(differentPage);
        var viewContext = CreateViewContext(Generated.NewBlank(), differentDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.TwoFactorAuthentication);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_TwoFactorAuthentication_WhitespaceActivePage_ReturnsNull()
    {
        // Arrange
        var whitespaceActivePage = Generated.NewWhitespaceValue();
        var differentPage = Generated.NewDifferentPageName();
        var differentDisplayName = PathEndingIn(differentPage);
        var viewContext = CreateViewContext(whitespaceActivePage, differentDisplayName);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.TwoFactorAuthentication);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_TwoFactorAuthentication_NullDisplayNameAndNoActivePage_ReturnsNull()
    {
        // Arrange
        var viewContext = CreateViewContext(activePage: null, displayName: null);

        // Act
        var result = ManageNavPages.PageNavClass(viewContext, ManageNavPages.TwoFactorAuthentication);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PageNavClass_TwoFactorAuthentication_NullViewContext_ThrowsArgumentNullException()
    {
        // Arrange
        ViewContext? viewContext = null;

        // Act
        var exception = Record.Exception(() => ManageNavPages.PageNavClass(viewContext, ManageNavPages.TwoFactorAuthentication));

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

    private static string PathEndingIn(string page) => JoinPath(Path.AltDirectorySeparatorChar, page);

    private static string WindowsPathEndingIn(string page) => JoinPath(ManageNavPages.WindowsDirectorySeparator, page);

    private static string JoinPath(char separator, string page) =>
        string.Join(separator, Generated.NewPathSegment(), Generated.NewPathSegment(), FileNameFor(page));
}
