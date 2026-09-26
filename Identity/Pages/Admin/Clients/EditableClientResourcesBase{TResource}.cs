namespace Identity.Pages.Admin.Clients;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public abstract class EditableClientResourcesBase<TResource> : EditableResourcesBase<int, TResource>
    where TResource : class, new()
{
    private readonly IConfigurationDbContext _context;

    protected EditableClientResourcesBase(IConfigurationDbContext context) => _context = context;

    public Client Client { get; private set; } = new();

    protected abstract Expression<Func<Client, List<TResource>>> Collection { get; }

    protected abstract string DetailsPage { get; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await FindClientWithResourcesAsync(id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        Resources = Collection.Compile().Invoke(client);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var client = await FindClientWithResourcesAsync(id);
        if (client is null)
        {
            return NotFound();
        }

        MergePostedResources(Collection.Compile().Invoke(client), id);
        client.Updated = DateTimeOffset.UtcNow.UtcDateTime;
        await _context.SaveChangesAsync();
        return RedirectToPage(DetailsPage, new { id });
    }

    protected abstract int IdOf(TResource resource);

    protected abstract void CopyEditableFields(TResource posted, TResource existing);

    protected abstract TResource CreateForClient(TResource posted, int clientId);

    protected override async Task<bool> LoadContextAsync(int id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return false;
        }

        Client = client;
        return true;
    }

    protected override TResource NewResource() => new();

    private Task<Client?> FindClientWithResourcesAsync(int id) =>
        _context.Clients
            .Include(Collection)
            .FirstOrDefaultAsync(c => c.Id == id);

    private void MergePostedResources(List<TResource> stored, int clientId)
    {
        var postedIds = Resources.Where(r => IdOf(r) > 0).Select(IdOf).ToHashSet();
        stored.RemoveAll(r => !postedIds.Contains(IdOf(r)));

        foreach (var posted in Resources.Where(r => IdOf(r) > 0))
        {
            var existing = stored.Find(r => IdOf(r) == IdOf(posted));
            if (existing is not null)
            {
                CopyEditableFields(posted, existing);
            }
        }

        foreach (var posted in Resources.Where(r => IdOf(r) == 0))
        {
            stored.Add(CreateForClient(posted, clientId));
        }
    }
}
