namespace Identity.Tests.Unit.Pages.Admin.SamlServiceProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.SamlServiceProviders;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class EditcshtmlTests
{
    private static readonly string EntityId = TestValues.NewEntityIdUrn();

    private static readonly string UpdatedEntityId = TestValues.NewEntityIdUrn();

    private static readonly string UnmatchedEntityId = TestValues.NewEntityIdUrn();

    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var sp = new SamlServiceProvider { Id = ExistingEntityId, EntityId = EntityId };
        var mockSet = MockDbSetHelper.BuildMockDbSet([sp]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.SamlServiceProviders).Returns(mockSet.Object);

        var model = new EditModel(ctx.Object);
        var result = await model.OnGetAsync(ExistingEntityId);

        Assert.IsType<PageResult>(result);
        Assert.Equal(EntityId, model.SamlServiceProvider.EntityId);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<SamlServiceProvider>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.SamlServiceProviders).Returns(mockSet.Object);

        Assert.IsType<NotFoundResult>(await new EditModel(ctx.Object).OnGetAsync(MissingEntityId));
    }

    [Fact]
    public async Task OnPostAsync_UpdatesAndRedirects_WhenValid()
    {
        var sp = new SamlServiceProvider { Id = ExistingEntityId, EntityId = EntityId };
        var mockSet = MockDbSetHelper.BuildMockDbSet([sp]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.SamlServiceProviders).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new EditModel(ctx.Object) { SamlServiceProvider = new SamlServiceProvider { EntityId = UpdatedEntityId, Enabled = true } };
        var result = await model.OnPostAsync(ExistingEntityId);

        Assert.Equal(UpdatedEntityId, sp.EntityId);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingDetails, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<SamlServiceProvider>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.SamlServiceProviders).Returns(mockSet.Object);

        var model = new EditModel(ctx.Object) { SamlServiceProvider = new SamlServiceProvider { EntityId = UnmatchedEntityId } };
        Assert.IsType<NotFoundResult>(await model.OnPostAsync(MissingEntityId));
    }
}