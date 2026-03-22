#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account.Manage;

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

/// <summary>
/// Tests for ManageNavPages static members.
/// </summary>
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

    /// <summary>
    /// Provides test cases covering:
    /// - ViewData['ActivePage'] as matching string (exact and case-different)
    /// - ViewData['ActivePage'] as non-matching string
    /// - ViewData['ActivePage'] missing or non-string causing fallback to ActionDescriptor.DisplayName
    /// - Both ViewData['ActivePage'] and DisplayName null
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Provides test cases for PersonalDataNavClass.
    /// Contains cases that exercise:
    /// - Direct ActivePage match (exact and different case)
    /// - Null ActivePage falling back to ActionDescriptor.DisplayName filename
    /// - Empty or whitespace ActivePage which prevents fallback and should not match
    /// - Very long and special-character ActivePage values that should not match
    /// - DisplayName values that do and do not produce the expected filename
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Verifies that the Index property returns the expected page name.
    /// Input: none (static property access).
    /// Expected: returns non-null, non-empty string equal to "Index".
    /// </summary>
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

    /// <summary>
    /// Verifies that ManageNavPages.ExternalLogins returns the expected literal value.
    /// Input conditions: no inputs (static property access).
    /// Expected result: the property returns the exact string "ExternalLogins", is not null, and is not whitespace.
    /// </summary>
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

    /// <summary>
    /// Test that when ViewData["ActivePage"] contains the page name (any casing),
    /// IndexNavClass returns "active".
    /// Inputs: ActivePage set to "Index" in various casings.
    /// Expected: method returns "active".
    /// </summary>
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

    /// <summary>
    /// Test that when ViewData["ActivePage"] is null and ActionDescriptor.DisplayName
    /// resolves (via Path.GetFileNameWithoutExtension) to the page name, IndexNavClass returns "active".
    /// Inputs: DisplayName values that should yield "Index" after filename extraction.
    /// Expected: method returns "active".
    /// </summary>
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

    /// <summary>
    /// Test that when neither ActivePage nor DisplayName indicate the Index page,
    /// IndexNavClass returns null.
    /// Inputs: ActivePage explicitly different and DisplayName null.
    /// Expected: method returns null.
    /// </summary>
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

    /// <summary>
    /// Test that when ViewData["ActivePage"] contains a non-string value, it is ignored (falls back to DisplayName).
    /// Inputs: ActivePage set to an integer object; DisplayName resolves to Index.
    /// Expected: method returns "active".
    /// </summary>
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

    /// <summary>
    /// The test verifies DeletePersonalDataNavClass returns the expected navigation class ("active" or null)
    /// for various combinations of ViewData["ActivePage"] values and ActionDescriptor.DisplayName values.
    /// Cases included:
    /// - ActivePage matches the DeletePersonalData page (case-insensitive) => "active".
    /// - ActivePage present but different => null (unless fallback file name matches).
    /// - ActivePage missing or not a string => fallback to file name derived from DisplayName.
    /// - DisplayName pointing to a file named DeletePersonalData.cshtml => "active" via fallback.
    /// - DisplayName null and no ActivePage => null.
    /// </summary>
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

    /// <summary>
    /// Verifies that passing a null ViewContext to DeletePersonalDataNavClass results in a NullReferenceException.
    /// Input: viewContext = null.
    /// Expected: NullReferenceException is thrown.
    /// </summary>
    [Fact]
    public void DeletePersonalDataNavClass_NullViewContext_ThrowsNullReferenceException()
    {
        // Arrange
        ViewContext? viewContext = null;

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => ManageNavPages.DeletePersonalDataNavClass(viewContext!));
    }

    /// <summary>
    /// Verifies PageNavClass returns "active" only when the resolved active page (from ViewData["ActivePage"] as string
    /// or the file name without extension of ActionDescriptor.DisplayName) equals the provided page string,
    /// using an ordinal, case-insensitive comparison. Various combinations of ActivePage (string, non-string, null)
    /// and DisplayName (path with extension, null) are exercised.
    /// </summary>
    /// <remarks>
    /// Arrange: build a ViewContext with ViewData["ActivePage"] set to the given activePage object and ActionDescriptor.DisplayName set.
    /// Act: call ManageNavPages.PageNavClass(viewContext, page).
    /// Assert: result equals expected (\"active\" or null).
    /// </remarks>
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

    /// <summary>
    /// Verifies that the DownloadPersonalData property returns the exact expected page name.
    /// Input condition: accessing the static DownloadPersonalData property.
    /// Expected result: returns the non-null, non-empty string literal "DownloadPersonalData".
    /// </summary>
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

    /// <summary>
    /// Verifies that the PersonalData property returns the expected literal value and basic string invariants.
    /// Input: no inputs (static property).
    /// Expected: returns the exact string "PersonalData", is non-null/non-empty, has expected length, contains no space characters and no control characters.
    /// </summary>
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

    /// <summary>
    /// Ensures the PersonalData property is stable across multiple accesses.
    /// Input: repeated accesses of the static property.
    /// Expected: value remains identical and reference-equality is allowed for interned strings.
    /// </summary>
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

    /// <summary>
    /// Tests EmailNavClass behavior for various combinations of ViewData['ActivePage'] and ActionDescriptor.DisplayName.
    /// Inputs:
    /// - activePageValue: object placed into ViewData['ActivePage'] (may be string, non-string, or null).
    /// - displayName: ActionDescriptor.DisplayName used when ViewData['ActivePage'] is not a string.
    /// Expected:
    /// - Returns "active" when the resolved active page (from ViewData or DisplayName filename) matches ManageNavPages.Email case-insensitively.
    /// - Returns null otherwise.
    /// </summary>
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

    /// <summary>
    /// Verifies ExternalLoginsNavClass returns "active" when the effective active page equals the ExternalLogins page name,
    /// and returns null otherwise. Tests combinations where ViewData["ActivePage"] is present (including case variations and
    /// empty/whitespace) and where it is absent so the ActionDescriptor.DisplayName file name is used.
    /// Inputs:
    /// - activePage: the value stored in viewContext.ViewData["ActivePage"] (nullable).
    /// - displayName: the ActionDescriptor.DisplayName (nullable).
    /// Expected:
    /// - "active" when the effective active page (ViewData value if non-null-string, otherwise file name without extension)
    ///   matches \"ExternalLogins\" ignoring case; otherwise null.
    /// </summary>
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

    /// <summary>
    /// Verifies that passing a null ViewContext to ExternalLoginsNavClass results in a NullReferenceException.
    /// The implementation accesses viewContext.ViewData and does not guard against null, so a NullReferenceException is expected.
    /// </summary>
    [Fact]
    public void ExternalLoginsNavClass_NullViewContext_ThrowsNullReferenceException()
    {
        // Arrange
        ViewContext? viewContext = null;

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => ManageNavPages.ExternalLoginsNavClass(viewContext!));
    }

    /// <summary>
    /// Verifies that the ChangePassword property returns the expected literal.
    /// Input: the expected literal string "ChangePassword".
    /// Expected: the static property returns a non-null, non-empty string exactly matching the expected value.
    /// </summary>
    /// <param name="expected">The expected literal value returned by the property.</param>
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

    /// <summary>
    /// Ensures that repeated accesses to the ChangePassword property return the same reference-equal string instance.
    /// Input: none.
    /// Expected: subsequent calls return equal strings (reference equality is not guaranteed by C#, but we still assert value equality and immutability).
    /// </summary>
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

    /// <summary>
    /// Verifies that the TwoFactorAuthentication property returns the expected literal string value.
    /// Input conditions: No inputs (static property).
    /// Expected result: The property returns the exact string "TwoFactorAuthentication" and is not null, empty, or whitespace.
    /// </summary>
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

    /// <summary>
    /// Tests that ChangePasswordNavClass returns "active" when either ViewData["ActivePage"]
    /// equals the ChangePassword page name (case-insensitive) or when the ActionDescriptor.DisplayName
    /// file name (without extension) equals the ChangePassword page name. Also verifies null is returned
    /// when neither source identifies the active page.
    /// Inputs:
    ///  - activePage: value placed into viewContext.ViewData["ActivePage"] (may be null)
    ///  - displayName: value assigned to ActionDescriptor.DisplayName (may be null)
    /// Expected:
    ///  - "active" or null according to the logic in PageNavClass.
    /// </summary>
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

    /// <summary>
    /// Verifies that when ViewData contains an ActivePage value that is whitespace-only,
    /// it does not match the ChangePassword page name and therefore returns null.
    /// This exercises a boundary/invalid string scenario for ActivePage.
    /// </summary>
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

    /// <summary>
    /// Tests PersonalDataNavClass with a variety of ActivePage and ActionDescriptor.DisplayName inputs.
    /// Verifies that when the active page (from ViewData["ActivePage"]) or the current action display name
    /// (file name without extension) matches ManageNavPages.PersonalData (case-insensitive) the method
    /// returns "active"; otherwise it returns null.
    /// </summary>
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

    /// <summary>
    /// Ensures that when ViewData[\"ActivePage\"] equals the Passkeys page name (case-insensitive),
    /// PasskeysNavClass returns "active".
    /// Input conditions: viewContext.ViewData contains ActivePage values provided by InlineData.
    /// Expected: "active" returned for matching values regardless of case.
    /// </summary>
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

    /// <summary>
    /// Ensures that when ViewData does not contain ActivePage, the ActionDescriptor.DisplayName filename
    /// (without extension) is used to determine active state. If it matches Passkeys, returns "active".
    /// Input conditions: ViewData has no ActivePage key; DisplayName contains a filename with extension.
    /// Expected: "active" when filename without extension equals Passkeys.
    /// </summary>
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

    /// <summary>
    /// Ensures that when neither ViewData[\"ActivePage\"] nor the ActionDescriptor filename match Passkeys,
    /// PasskeysNavClass returns null.
    /// Input conditions: ActivePage set to a different value and DisplayName filename also different.
    /// Expected: null returned.
    /// </summary>
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

    /// <summary>
    /// Ensures that calling PasskeysNavClass with a null ViewContext results in a NullReferenceException.
    /// Input conditions: viewContext is null.
    /// Expected: NullReferenceException thrown.
    /// </summary>
    [Fact]
    public void PasskeysNavClass_NullViewContext_ThrowsNullReferenceException()
    {
        // Arrange
        ViewContext? viewContext = null;

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => ManageNavPages.PasskeysNavClass(viewContext!));
    }

    /// <summary>
    /// Verifies that the Email property returns the expected literal value.
    /// Input conditions: no inputs (static property access).
    /// Expected result: returns non-null, non-empty string equal to "Email" and length matches the expected literal.
    /// </summary>
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

    /// <summary>
    /// Verifies that the DeletePersonalData property returns the expected constant value.
    /// Input conditions: no inputs (static property).
    /// Expected result: the property is non-null, non-whitespace, equals the literal "DeletePersonalData", and has the expected length.
    /// </summary>
    /// <param name="expected">Expected string value.</param>
    /// <param name="expectedLength">Expected length of the returned string.</param>
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

    /// <summary>
    /// Verifies that the Passkeys property returns the expected literal value.
    /// Input conditions: direct access to the static Passkeys property.
    /// Expected result: returns the string "Passkeys" and is not null/whitespace.
    /// </summary>
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

    /// <summary>
    /// Ensures repeated accesses to Passkeys produce the same value and are reference-consistent.
    /// Input conditions: a variable number of repeated reads (provided by InlineData).
    /// Expected result: every read equals "Passkeys" and is the same reference as the first read.
    /// </summary>
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

    /// <summary>
    /// Verifies that DownloadPersonalDataNavClass returns "active" when the active page (from ViewData["ActivePage"])
    /// or the action descriptor's file name (DisplayName) matches the DownloadPersonalData page name (case-insensitive).
    /// Also verifies null is returned when neither value matches.
    /// Test cases:
    /// - activePage equals the page name (exact match) => "active"
    /// - activePage equals the page name (different case) => "active"
    /// - activePage is null and DisplayName contains the page file name => "active"
    /// - activePage set to a different page while DisplayName contains the page file name => null (ActivePage takes precedence)
    /// - both activePage and DisplayName are null => null
    /// </summary>
    /// <param name="activePage">Value to place into ViewData["ActivePage"] (may be null to force fallback).</param>
    /// <param name="actionDisplayName">ActionDescriptor.DisplayName to use for fallback (may be null).</param>
    /// <param name="expected">Expected result: "active" or null.</param>
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

    /// <summary>
    /// Verifies that when ViewData["ActivePage"] matches (case-insensitive) the TwoFactorAuthentication page name,
    /// the nav class returned is "active".
    /// Input conditions: viewContext.ViewData["ActivePage"] is provided with varying casing.
    /// Expected result: "active" is returned.
    /// </summary>
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

    /// <summary>
    /// Verifies that when ViewData lacks "ActivePage" (null or absent) and ActionDescriptor.DisplayName
    /// corresponds to a file name matching TwoFactorAuthentication (with extension), the nav class returned is "active".
    /// Input conditions: ViewData does not contain "ActivePage"; DisplayName ends with "TwoFactorAuthentication.cshtml".
    /// Expected result: "active" is returned.
    /// </summary>
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

    /// <summary>
    /// Verifies that when ViewData["ActivePage"] is a whitespace or empty string and DisplayName does not match,
    /// the nav class returned is null.
    /// Input conditions: ViewData["ActivePage"] is empty or whitespace; DisplayName is unrelated.
    /// Expected result: null is returned.
    /// </summary>
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

    /// <summary>
    /// Verifies that when both ViewData["ActivePage"] is null and ActionDescriptor.DisplayName is null,
    /// the nav class returned is null.
    /// Input conditions: ViewData does not contain "ActivePage"; DisplayName is null.
    /// Expected result: null is returned.
    /// </summary>
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

    /// <summary>
    /// Verifies that passing a null ViewContext results in a NullReferenceException.
    /// Input conditions: viewContext is null.
    /// Expected result: NullReferenceException is thrown.
    /// </summary>
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