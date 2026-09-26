namespace Identity.Tests.Unit.Pages.Admin.IdentityProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.IdentityProviders;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class EditTests
{
    private static readonly string SchemeName = Generated.NewSchemeName();

    private static readonly string UpdatedSchemeName = Generated.NewSchemeName();

    private static readonly string UnmatchedSchemeName = Generated.NewSchemeName();

    private static readonly int ExistingEntityId = Generated.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var provider = new IdentityProvider { Id = ExistingEntityId, Scheme = SchemeName };
        var mockSet = MockDbSetHelper.BuildMockDbSet([provider]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityProviders).Returns(mockSet.Object);
        var model = new Edit(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(SchemeName, model.IdentityProvider.Scheme);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<IdentityProvider>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityProviders).Returns(mockSet.Object);
        var model = new Edit(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesAndRedirects_WhenValid()
    {
        // Arrange
        var provider = new IdentityProvider { Id = ExistingEntityId, Scheme = SchemeName };
        var mockSet = MockDbSetHelper.BuildMockDbSet([provider]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityProviders).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new Edit(ctx.Object) { IdentityProvider = new IdentityProvider { Scheme = UpdatedSchemeName, DisplayName = Generated.NewClientName() } };

        // Act
        var result = await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.Equal(UpdatedSchemeName, provider.Scheme);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingDetails, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<IdentityProvider>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.IdentityProviders).Returns(mockSet.Object);
        var model = new Edit(ctx.Object) { IdentityProvider = new IdentityProvider { Scheme = UnmatchedSchemeName } };

        // Act
        var result = await model.OnPostAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
