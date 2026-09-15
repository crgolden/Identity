namespace Identity.Tests.Unit.Pages.Admin.IdentityResources.Edit;

using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.IdentityResources.Edit;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    private static readonly string UpdatedResourceName = TestValues.NewScopeName();

    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var resource = new IdentityResource { Id = ExistingEntityId, Name = IdentityServerConstants.StandardScopes.OpenId };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityResources).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(IdentityServerConstants.StandardScopes.OpenId, model.Resource.Name);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<IdentityResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityResources).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesAndRedirects_WhenValid()
    {
        // Arrange
        var resource = new IdentityResource { Id = ExistingEntityId, Name = IdentityServerConstants.StandardScopes.OpenId };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityResources).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new IndexModel(ctx.Object) { Resource = new IdentityResource { Name = UpdatedResourceName, DisplayName = TestValues.NewClientName() } };

        // Act
        var result = await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.Equal(UpdatedResourceName, resource.Name);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(IndexModel.DetailsPageName, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<IdentityResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityResources).Returns(mockSet.Object);
        var model = new IndexModel(ctx.Object) { Resource = new IdentityResource { Name = TestValues.NewApiResourceName() } };

        // Act
        var result = await model.OnPostAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}