namespace Identity.Tests.Filters;
using Identity.Tests.Infrastructure;

using Identity.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

/// <summary>Unit tests for <see cref="SecurityHeadersAttribute"/>.</summary>
[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class SecurityHeadersAttributeTests
{
    /// <summary>
    /// Verifies that OnResultExecuting sets X-Content-Type-Options to "nosniff" when the result is a PageResult.
    /// Input: ResultExecutingContext with PageResult.
    /// Expected: X-Content-Type-Options header equals "nosniff".
    /// </summary>
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

    /// <summary>
    /// Verifies that OnResultExecuting sets X-Frame-Options to "SAMEORIGIN" when the result is a PageResult.
    /// Input: ResultExecutingContext with PageResult.
    /// Expected: X-Frame-Options header equals "SAMEORIGIN".
    /// </summary>
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

    /// <summary>
    /// Verifies that OnResultExecuting sets Referrer-Policy to "no-referrer" when the result is a PageResult.
    /// Input: ResultExecutingContext with PageResult.
    /// Expected: Referrer-Policy header equals "no-referrer".
    /// </summary>
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

    /// <summary>
    /// Verifies that OnResultExecuting sets Content-Security-Policy to the default strict value when the result is a PageResult.
    /// Input: ResultExecutingContext with PageResult and no pre-existing CSP header.
    /// Expected: Content-Security-Policy header equals the expected default policy string.
    /// </summary>
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

    /// <summary>
    /// Verifies that OnResultExecuting does not set any security headers when the result is not a PageResult.
    /// Input: ResultExecutingContext with ContentResult (not a PageResult).
    /// Expected: none of the four security headers are present in the response.
    /// </summary>
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

    /// <summary>
    /// Verifies that OnResultExecuting does not overwrite an existing Content-Security-Policy header,
    /// while still setting the other three headers.
    /// Input: ResultExecutingContext with PageResult and a pre-existing CSP header value.
    /// Expected: CSP retains its original value; X-Content-Type-Options, X-Frame-Options, and Referrer-Policy are set.
    /// </summary>
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

    /// <summary>
    /// Verifies that calling OnResultExecuting twice on the same context leaves headers at their expected values.
    /// The second call must not overwrite the X-Content-Type-Options, X-Frame-Options, or Referrer-Policy headers
    /// with a different value, and must not overwrite the CSP header set by the first call.
    /// Input: PageResult context; filter called twice in sequence.
    /// Expected: all four headers equal the expected values after both calls.
    /// </summary>
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
