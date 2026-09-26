namespace Identity.Pages;

using Microsoft.AspNetCore.Mvc.RazorPages;

public abstract class ResourcesBase<TResource> : PageModel
{
    public IList<TResource> Resources { get; protected set; } = [];
}
