#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account.Manage;
using Infrastructure;

using Identity.Pages.Account.Manage;
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
        // activePageValue, hasActivePage, displayName, expectedResult
        // 1) ViewData ActivePage exactly matches the page => active
        { "DeletePersonalData", true, "/some/path/Irrelevant.cshtml", "active" },

        // 2) Case-insensitive match => active
        { "deletepersonaldata", true, "/some/path/Irrelevant.cshtml", "active" },

        // 3) ActivePage present but different; DisplayName file name equals DeletePersonalData => fallback used => active
        { "SomethingElse", true, "/Areas/Identity/Pages/Account/Manage/DeletePersonalData.cshtml", null },

        // 4) No ActivePage key; DisplayName filename is DeletePersonalData => fallback should yield active
        { null, false, "/Areas/Identity/Pages/Account/Manage/DeletePersonalData.cshtml", "active" },

        // 5) ActivePage exists but is non-string (int): 'as string' yields null -> fallback to DisplayName filename
        { 123, true, "/Areas/Identity/Pages/Account/Manage/DeletePersonalData.cshtml", "active" },

        // 6) No ActivePage and DisplayName null => fallback is null => result null
        { null, false, null, null },
    };

    public static TheoryData<object?, string?, string, string?> PageNavTestData()
    {
        var longStr = new string('a', 600);
        return new TheoryData<object?, string?, string, string?>
        {
            // ActivePage is a string and matches exactly -> active
            { "Index", "/Areas/Identity/Pages/Account/Manage/Index.cshtml", "Index", "active" },

            // ActivePage is a different casing -> still active (case-insensitive)
            { "index", "/Some/Path/Index.cshtml", "Index", "active" },

            // ActivePage empty string matches empty page
            { string.Empty, "/Any/Path/Ignore.cshtml", string.Empty, "active" },

            // ActivePage is null -> fallback to DisplayName file name, matches -> active
            { null, "/Areas/Identity/Pages/Account/Manage/ChangePassword.cshtml", "ChangePassword", "active" },

            // ActivePage is non-string (int) -> 'as string' yields null -> fallback to DisplayName file name, matches -> active
            { 123, "/some/path/Custom-Page.cshtml", "Custom-Page", "active" },

            // ActivePage string exists but does not match page -> should be null even though DisplayName would match
            { "OtherPage", "/path/Index.cshtml", "Index", null },

            // ActivePage null and DisplayName null -> no match -> null
            { null, null, "Index", null },

            // ActivePage null and DisplayName has file name with special characters -> matches
            { null, "/x/y/special_name!@#$.cshtml", "special_name!@#$", "active" },

            // ActivePage has whitespace-only string and page matches same whitespace -> active
            { "   ", "/ignored/path.cshtml", "   ", "active" },

            // Long string match (boundary test)
            { longStr, "/ignored/long.cshtml", longStr, "active" },

            // DisplayName contains no directory, just filename -> fallback extracts name
            { null, "PlainName.cshtml", "PlainName", "active" },
        };
    }

    public static TheoryData<object?, string?, string?> EmailNavClassCases() => new()
    {
        // When ViewData contains a matching page name (exact)
        { "Email", null, "active" },

        // When ViewData contains a matching page name but different case (case-insensitive match)
        { "email", null, "active" },

        // When ViewData contains a non-matching page name -> not active
        { "Other", null, null },

        // When ViewData does not contain a string (null) but DisplayName filename matches
        { null, "/Pages/Account/Manage/Email.cshtml", "active" },

        // When ViewData does not contain a string but DisplayName filename does not match
        { null, "/Pages/Account/Manage/Other.cshtml", null },

        // When ViewData contains a non-string object -> treated as null, fallback to DisplayName
        { 123, "/Pages/Account/Manage/Email.cshtml", "active" },

        // When both ViewData['ActivePage'] and DisplayName are null -> no active page
        { null, null, null },

        // When DisplayName filename differs only by case -> should be active (case-insensitive)
        { null, "/Pages/Account/Manage/EMAIL.CSHTML", "active" },

        // When ViewData contains whitespace-only string -> does not match (whitespace != "Email", no fallback to DisplayName)
        { "   ", "/Pages/Account/Manage/Email.cshtml", null },
    };

    public static TheoryData<string?, string?, string?> PageCases() => new()
    {
        // activePage, displayName, expectedResult
        { "ChangePassword", null, "active" }, // exact match in ActivePage
        { "changepassword", null, "active" }, // case-insensitive match in ActivePage
        { null, "/Views/Account/Manage/ChangePassword.cshtml", "active" }, // DisplayName filename matches page
        { null, "/Views/Account/Manage/Other.cshtml", null }, // DisplayName filename does not match page
        { null, null, null }, // both sources null -> no active
    };

    public static TheoryData<string?, string?, string?> GetPersonalDataNavCases() => new()
    {
        // ActivePage exactly matches -> active
        { "PersonalData", null, "active" },

        // ActivePage matches ignoring case -> active
        { "personaldata", null, "active" },

        // ActivePage null, DisplayName contains PersonalData filename -> active
        { null, "Pages/Account/Manage/PersonalData.cshtml", "active" },

        // ActivePage null, DisplayName with full path -> active
        { null, "C:\\Views\\Account\\Manage\\PersonalData.cshtml", "active" },

        // ActivePage null, DisplayName null -> no active (Path.GetFileNameWithoutExtension returns null)
        { null, null, null },

        // ActivePage empty string prevents fallback and does not match -> null
        { string.Empty, "Pages/Account/Manage/PersonalData.cshtml", null },

        // ActivePage whitespace prevents fallback and should not match -> null
        { "   ", null, null },

        // ActivePage close but with trailing space -> not equal -> null
        { "PersonalData ", null, null },

        // Very long ActivePage -> null
        { new string('x', 1000), null, null },

        // ActivePage with special characters -> null
        { "PersonalData\u2603", null, null },

        // DisplayName filename differs (contains prefix) -> null
        { null, "Pages/Account/Manage/some.PersonalData.cshtml", null },
    };

    [Theory]
    [InlineData("Index")]
    public void Index_Property_ReturnsExpected(string expected)
    {
        // Arrange
        // (No setup required for static property.)

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
        // (No setup required for a static string property.)

        // Act
        var actual = ManageNavPages.ExternalLogins;

        // Assert
        Assert.NotNull(actual); // property must not be null
        Assert.False(string.IsNullOrWhiteSpace(actual)); // property must not be empty or whitespace
        Assert.Equal(expected, actual); // exact match expected
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

        var mockView = new Mock<IView>();
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

        // ActivePage intentionally not set (null)
        var mockView = new Mock<IView>();
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

        var mockView = new Mock<IView>();
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
            ["ActivePage"] = 123 // non-string value; 'as string' should yield null
        };

        var mockView = new Mock<IView>();
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
            // Set the ActivePage entry to the provided value (can be string or non-string)
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

        // Put the object into ViewData to simulate non-string values as well
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
        // (No setup required for a static literal property.)

        // Act
        var result = ManageNavPages.DownloadPersonalData;

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result)); // Not null or empty
        Assert.Equal(expected, result); // Exact literal match
    }

    [Theory]
    [InlineData("PersonalData", 12)]
    public void PersonalData_Property_ReturnsExpected(string expected, int expectedLength)
    {
        // Arrange
        // (no arrangement needed for a static literal property)

        // Act
        var actual = ManageNavPages.PersonalData;

        // Assert
        Assert.NotNull(actual); // not null
        Assert.NotEmpty(actual); // not empty
        Assert.Equal(expected, actual); // exact expected value
        Assert.Equal(expectedLength, actual.Length); // expected length

        // No whitespace characters (space, tabs, newlines)
        Assert.DoesNotContain(" ", actual);
        Assert.DoesNotContain("\t", actual);
        Assert.DoesNotContain("\n", actual);
        Assert.DoesNotContain("\r", actual);

        // No control characters
        foreach (var c in actual)
        {
            Assert.False(char.IsControl(c), $"Unexpected control character U+{(int)c:X4} in PersonalData value.");
        }
    }

    [Fact]
    public void PersonalData_Property_IsStableAcrossAccesses()
    {
        // Arrange
        // (no arrangement required)

        // Act
        var first = ManageNavPages.PersonalData;
        var second = ManageNavPages.PersonalData;
        var third = ManageNavPages.PersonalData;

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(second, third);

        // Optionally assert reference equality because literals are interned by the CLR,
        // but do not require it for correctness; if interned, reference equality will hold.
        // Verify reference equality as a supplementary check without making it required for correctness.
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

    // ActivePage matches exactly -> active
    [InlineData("ExternalLogins", null, "active")]

    // ActivePage matches case-insensitively -> active
    [InlineData("externallogins", null, "active")]

    // ActivePage absent; DisplayName filename matches -> active
    [InlineData(null, "Areas/Identity/Pages/Account/Manage/ExternalLogins.cshtml", "active")]

    // ActivePage absent; DisplayName filename differs -> null
    [InlineData(null, "Areas/Identity/Pages/Account/Manage/SomeOther.cshtml", null)]

    // ActivePage present but empty -> should not fall back to DisplayName and should be treated as not matching -> null
    [InlineData("", "Areas/Identity/Pages/Account/Manage/ExternalLogins.cshtml", null)]

    // ActivePage present with surrounding whitespace -> does not equal the page value -> null
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
        // (No setup required for a static literal property.)

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
        // (No setup required for a static literal property.)

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
        var viewMock = new Mock<IView>();
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
        var view = new Mock<IView>().Object;
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

        var mockView = new Mock<IView>();
        var view = mockView.Object;

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            ["ActivePage"] = activePage
        };

        var tempData = new TempDataDictionary(actionContext.HttpContext, new Mock<ITempDataProvider>().Object);
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

        var mockView = new Mock<IView>();
        var view = mockView.Object;

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());

        // Note: Do not set ViewData["ActivePage"] to force fallback to ActionDescriptor.DisplayName
        var tempData = new TempDataDictionary(actionContext.HttpContext, new Mock<ITempDataProvider>().Object);
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

        var mockView = new Mock<IView>();
        var view = mockView.Object;

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            ["ActivePage"] = "DifferentPage"
        };

        var tempData = new TempDataDictionary(actionContext.HttpContext, new Mock<ITempDataProvider>().Object);
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
        // (no setup required for static literal property)

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
        // (No setup required for a static constant string property.)

        // Act
        var actual = ManageNavPages.DeletePersonalData;

        // Assert
        Assert.NotNull(actual); // Should never be null for this static constant.
        Assert.False(string.IsNullOrWhiteSpace(actual)); // Ensure it's meaningful content.
        Assert.Equal(expected, actual); // Exact match to the expected literal.
        Assert.Equal(expectedLength, actual.Length); // Length boundary check.
    }

    [Fact]
    public void Passkeys_Property_ReturnsExpected()
    {
        // Arrange
        // (No setup required for a static literal property)

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
        // (No external dependencies required; this verifies property stability)

        // Act
        var first = ManageNavPages.Passkeys;
        for (var i = 0; i < readCount; i++)
        {
            var current = ManageNavPages.Passkeys;

            // Assert inside loop for clearer failure localization
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

    // Helper to construct a ViewContext matching the expectations of ManageNavPages.PageNavClass.
    private static ViewContext CreateViewContext(string? activePage, string? displayName)
    {
        // ActionContext
        var httpContext = new DefaultHttpContext();
        var routeData = new RouteData();
        var actionDescriptor = new ActionDescriptor { DisplayName = displayName };
        var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);

        // ViewData: use EmptyModelMetadataProvider per common usage
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        if (activePage is not null)
        {
            // The code uses 'as string' so storing a string is correct; storing null (or omitting) simulates absence.
            viewData["ActivePage"] = activePage;
        }

        // TempData: a TempDataDictionary requires an ITempDataProvider
        var tempDataProviderMock = new Mock<ITempDataProvider>();
        var tempData = new TempDataDictionary(httpContext, tempDataProviderMock.Object);

        // IView can be mocked; its instance is not used by the method under test
        var viewMock = new Mock<IView>();

        // Writer and HtmlHelperOptions for the ViewContext constructor
        var writer = new StringWriter();
        var htmlHelperOptions = new HtmlHelperOptions();

        return new ViewContext(actionContext, viewMock.Object, viewData, tempData, writer, htmlHelperOptions);
    }
}