namespace Identity.Tests.Unit.Pages;

using Identity.Pages;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class RedirectTests
{
    private static readonly string ExternalUrl = Generated.NewOrigin();

    private static readonly string LocalPath = Generated.NewLocalPath();

    [Fact]
    public void Constructor_NoParameters_DoesNotThrow()
    {
        // Act
        var model = new Redirect();

        // Assert
        Assert.NotNull(model);
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public void OnGet_NonLocalUrl_RedirectsToError()
    {
        // Arrange
        var mockUrlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
        mockUrlHelper
            .Setup(x => x.IsLocalUrl(ExternalUrl))
            .Returns(false);

        var model = Create();
        model.Url = mockUrlHelper.Object;

        // Act
        var result = model.OnGet(ExternalUrl);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.Error, redirect.PageName);
    }

    [Fact]
    public void OnGet_LocalUrl_SetsRedirectUriAndReturnsPage()
    {
        // Arrange
        var mockUrlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
        mockUrlHelper
            .Setup(x => x.IsLocalUrl(LocalPath))
            .Returns(true);

        var model = Create();
        model.Url = mockUrlHelper.Object;

        // Act
        var result = model.OnGet(LocalPath);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(LocalPath, model.RedirectUri);
    }

    private static Redirect Create()
    {
        var model = new Redirect();
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
