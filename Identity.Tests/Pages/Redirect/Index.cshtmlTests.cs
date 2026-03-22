#pragma warning disable CS8604
#pragma warning disable CS8625
namespace Identity.Tests.Pages.Redirect;

using Identity.Pages.Redirect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;

/// <summary>Unit tests for <see cref="Identity.Pages.Redirect.IndexModel"/>.</summary>
[Trait("Category", "Unit")]
public class RedirectIndexModelTests
{
    /// <summary>
    /// Verifies that IndexModel can be constructed without parameters and does not throw.
    /// Inputs: no constructor arguments.
    /// Expected: no exception is thrown and the constructed instance is not null.
    /// </summary>
    [Fact]
    public void Constructor_NoParameters_DoesNotThrow()
    {
        // Arrange & Act
        IndexModel model = null!;
        var ex = Record.Exception(() => model = new IndexModel());

        // Assert
        Assert.Null(ex);
        Assert.NotNull(model);
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    /// <summary>
    /// Verifies that OnGet redirects to the error page when the redirect URI is not a local URL.
    /// Inputs: redirectUri = "https://external.com", Url.IsLocalUrl returns false.
    /// Expected: result is RedirectToPageResult with page "/Error".
    /// </summary>
    [Fact]
    public void OnGet_NonLocalUrl_RedirectsToError()
    {
        // Arrange
        var mockUrlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
        mockUrlHelper
            .Setup(x => x.IsLocalUrl("https://external.com"))
            .Returns(false);

        var model = CreateModel();
        model.Url = mockUrlHelper.Object;

        // Act
        var result = model.OnGet("https://external.com");

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Error", redirect.PageName);
    }

    /// <summary>
    /// Verifies that OnGet sets RedirectUri and returns PageResult when the URI is a local URL.
    /// Inputs: redirectUri = "/local/path", Url.IsLocalUrl returns true.
    /// Expected: result is PageResult and model.RedirectUri equals "/local/path".
    /// </summary>
    [Fact]
    public void OnGet_LocalUrl_SetsRedirectUriAndReturnsPage()
    {
        // Arrange
        var mockUrlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
        mockUrlHelper
            .Setup(x => x.IsLocalUrl("/local/path"))
            .Returns(true);

        var model = CreateModel();
        model.Url = mockUrlHelper.Object;

        // Act
        var result = model.OnGet("/local/path");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("/local/path", model.RedirectUri);
    }

    private static IndexModel CreateModel()
    {
        var model = new IndexModel();
        var httpContext = new DefaultHttpContext();
        model.PageContext = new PageContext
        {
            ActionDescriptor = new CompiledPageActionDescriptor(),
            HttpContext = httpContext,
            RouteData = new RouteData(),
        };
        return model;
    }
}
