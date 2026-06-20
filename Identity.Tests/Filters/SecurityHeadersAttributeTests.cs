namespace Identity.Tests.Filters;
using Infrastructure;

using Identity.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class SecurityHeadersAttributeTests
{
    [Fact]
    public void OnResultExecuting_PageResult_SetsXContentTypeOptionsNosniff()
    {
        // Arrange
        var context = MakeContext(new PageResult());
        var filter = new SecurityHeadersAttribute();

        // Act
        filter.OnResultExecuting(context);

        // Assert
        Assert.Equal("nosniff", (string?)context.HttpContext.Response.Headers.XContentTypeOptions);
    }

    [Fact]
    public void OnResultExecuting_PageResult_SetsXFrameOptionsSameorigin()
    {
        // Arrange
        var context = MakeContext(new PageResult());
        var filter = new SecurityHeadersAttribute();

        // Act
        filter.OnResultExecuting(context);

        // Assert
        Assert.Equal("SAMEORIGIN", (string?)context.HttpContext.Response.Headers.XFrameOptions);
    }

    [Fact]
    public void OnResultExecuting_PageResult_SetsReferrerPolicyNoReferrer()
    {
        // Arrange
        var context = MakeContext(new PageResult());
        var filter = new SecurityHeadersAttribute();

        // Act
        filter.OnResultExecuting(context);

        // Assert
        Assert.Equal("no-referrer", (string?)context.HttpContext.Response.Headers["Referrer-Policy"]);
    }

    [Fact]
    public void OnResultExecuting_PageResult_SetsDefaultContentSecurityPolicy()
    {
        // Arrange
        var context = MakeContext(new PageResult());
        var filter = new SecurityHeadersAttribute();

        // Act
        filter.OnResultExecuting(context);

        // Assert
        Assert.Equal(
            "default-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'self';",
            (string?)context.HttpContext.Response.Headers.ContentSecurityPolicy);
    }

    [Fact]
    public void OnResultExecuting_NonPageResult_DoesNotSetAnyHeaders()
    {
        // Arrange
        var context = MakeContext(new ContentResult());
        var filter = new SecurityHeadersAttribute();

        // Act
        filter.OnResultExecuting(context);

        // Assert
        var headers = context.HttpContext.Response.Headers;
        Assert.False(headers.ContainsKey("X-Content-Type-Options"));
        Assert.False(headers.ContainsKey("X-Frame-Options"));
        Assert.False(headers.ContainsKey("Referrer-Policy"));
        Assert.False(headers.ContainsKey("Content-Security-Policy"));
    }

    [Fact]
    public void OnResultExecuting_PageResult_ExistingCspNotOverwritten()
    {
        // Arrange
        var context = MakeContext(new PageResult());
        context.HttpContext.Response.Headers.ContentSecurityPolicy = "script-src 'none'";
        var filter = new SecurityHeadersAttribute();

        // Act
        filter.OnResultExecuting(context);

        // Assert — CSP is unchanged
        Assert.Equal("script-src 'none'", (string?)context.HttpContext.Response.Headers.ContentSecurityPolicy);

        // Other headers are still applied
        Assert.Equal("nosniff", (string?)context.HttpContext.Response.Headers.XContentTypeOptions);
        Assert.Equal("SAMEORIGIN", (string?)context.HttpContext.Response.Headers.XFrameOptions);
        Assert.Equal("no-referrer", (string?)context.HttpContext.Response.Headers["Referrer-Policy"]);
    }

    [Fact]
    public void OnResultExecuting_PageResult_CalledTwice_HeaderValuesUnchanged()
    {
        // Arrange
        var context = MakeContext(new PageResult());
        var filter = new SecurityHeadersAttribute();

        // Act
        filter.OnResultExecuting(context);
        filter.OnResultExecuting(context);

        // Assert
        Assert.Equal("nosniff", (string?)context.HttpContext.Response.Headers.XContentTypeOptions);
        Assert.Equal("SAMEORIGIN", (string?)context.HttpContext.Response.Headers.XFrameOptions);
        Assert.Equal("no-referrer", (string?)context.HttpContext.Response.Headers["Referrer-Policy"]);
        Assert.Equal(
            "default-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'self';",
            (string?)context.HttpContext.Response.Headers.ContentSecurityPolicy);
    }

    private static ResultExecutingContext MakeContext(IActionResult result)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor());
        return new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), result, new object());
    }
}
