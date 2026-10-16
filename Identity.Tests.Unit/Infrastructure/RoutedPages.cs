namespace Identity.Tests.Unit.Infrastructure;

using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

internal static class RoutedPages
{
    internal static void Record(UrlRouteContext context, List<string> routedPages)
    {
        if (context.Values is RouteValueDictionary values && values.TryGetValue(AspNetRouteConstants.PageRouteValueName, out var page) && page is string pageName)
        {
            routedPages.Add(pageName);
        }
    }
}
