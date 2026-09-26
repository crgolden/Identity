namespace Identity.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public abstract class EditableResourcesBase<TKey, TResource> : PageModel
{
    [BindProperty]
    public List<TResource> Resources { get; set; } = [];

    public async Task<IActionResult> OnPostAddRowAsync(TKey id)
    {
        if (!await LoadContextAsync(id))
        {
            return NotFound();
        }

        Resources.Add(NewResource());
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveRowAsync(TKey id, int index)
    {
        if (!await LoadContextAsync(id))
        {
            return NotFound();
        }

        if (index >= 0 && index < Resources.Count)
        {
            Resources.RemoveAt(index);
        }

        return Page();
    }

    protected abstract Task<bool> LoadContextAsync(TKey id);

    protected abstract TResource NewResource();
}
