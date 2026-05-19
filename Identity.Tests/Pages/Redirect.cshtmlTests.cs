#pragma warning disable CS8604
#pragma warning disable CS8625
namespace Identity.Tests.Pages;
using Infrastructure;

using Identity.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class RedirectIndexModelTests
{
    [Fact]
    public void Constructor_NoParameters_DoesNotThrow()
    {
        // Arrange & Act
        RedirectModel model = null!;
        var ex = Record.Exception(() => model = new RedirectModel());

        // Assert
        Assert.Null(ex);
        Assert.NotNull(model);
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

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

    private static RedirectModel CreateModel()
    {
        var model = new RedirectModel();
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
