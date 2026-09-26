namespace Identity.Tests.Unit.Pages.Admin.IdentityResources.Edit;

using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.IdentityResources.Edit;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ClaimTypesTests
{
    private static readonly string ClaimType = Generated.NewClaimType();

    private static readonly int ExistingEntityId = Generated.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var resource = new IdentityResource { Id = ExistingEntityId, Name = IdentityServerConstants.StandardScopes.OpenId, UserClaims = [new IdentityResourceClaim { Type = ClaimType }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityResources).Returns(mockSet.Object);
        var model = new ClaimTypes(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.Resources);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<IdentityResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityResources).Returns(mockSet.Object);
        var model = new ClaimTypes(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewClaimType()
    {
        // Arrange
        var resource = new IdentityResource { Id = ExistingEntityId, Name = IdentityServerConstants.StandardScopes.OpenId, UserClaims = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityResources).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new ClaimTypes(ctx.Object)
        {
            Resources = [new IdentityResourceClaim { Id = 0, Type = ClaimType }],
        };

        // Act
        var result = await model.OnPostAsync(ExistingEntityId);

        // Assert
        var onlyUserClaim = Assert.Single(resource.UserClaims);
        Assert.Equal(ClaimType, onlyUserClaim.Type);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(ClaimTypes.DetailsPageName, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<IdentityResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityResources).Returns(mockSet.Object);
        var model = new ClaimTypes(ctx.Object) { Resources = [] };

        // Act
        var result = await model.OnPostAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesAbsentClaimType()
    {
        // Arrange
        var existing = new IdentityResourceClaim { Id = ExistingEntityId, Type = ClaimType, IdentityResourceId = ExistingEntityId };
        var resource = new IdentityResource { Id = ExistingEntityId, Name = IdentityServerConstants.StandardScopes.OpenId, UserClaims = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityResources).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new ClaimTypes(ctx.Object) { Resources = [] };

        // Act
        await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.Empty(resource.UserClaims);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        // Arrange
        var resource = new IdentityResource { Id = ExistingEntityId, Name = IdentityServerConstants.StandardScopes.OpenId };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityResources).Returns(mockSet.Object);
        var model = new ClaimTypes(ctx.Object) { Resources = [] };

        // Act
        var result = await model.OnPostAddRowAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.Resources);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<IdentityResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityResources).Returns(mockSet.Object);
        var model = new ClaimTypes(ctx.Object) { Resources = [] };

        // Act
        var result = await model.OnPostAddRowAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        // Arrange
        var resource = new IdentityResource { Id = ExistingEntityId, Name = IdentityServerConstants.StandardScopes.OpenId };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityResources).Returns(mockSet.Object);
        var model = new ClaimTypes(ctx.Object) { Resources = [new IdentityResourceClaim { Id = ExistingEntityId, Type = ClaimType }] };

        // Act
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, 0);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Resources);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<IdentityResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityResources).Returns(mockSet.Object);
        var model = new ClaimTypes(ctx.Object) { Resources = [] };

        // Act
        var result = await model.OnPostRemoveRowAsync(MissingEntityId, 0);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
