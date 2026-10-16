namespace Identity.Tests.Unit.Pages.Admin.ApiResources.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.ApiResources.Edit;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ScopesTests
{
    private static readonly string ExistingScopeName = Generated.NewApiScopeName();

    private static readonly string PostedScopeName = Generated.NewApiScopeName();

    private static readonly int ExistingEntityId = Generated.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var resource = new ApiResource { Id = ExistingEntityId, Name = Generated.NewApiResourceName(), Scopes = [new ApiResourceScope { Id = ExistingEntityId, Scope = ExistingScopeName }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        var model = new Scopes(ctx.Object);

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
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        var model = new Scopes(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewScope()
    {
        // Arrange
        var resource = new ApiResource { Id = ExistingEntityId, Name = Generated.NewApiResourceName(), Scopes = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new Scopes(ctx.Object) { Resources = [new ApiResourceScope { Id = 0, Scope = PostedScopeName }] };

        // Act
        var result = await model.OnPostAsync(ExistingEntityId);

        // Assert
        var onlyScope = Assert.Single(resource.Scopes);
        Assert.Equal(PostedScopeName, onlyScope.Scope);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(Scopes.DetailsPageName, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        var model = new Scopes(ctx.Object) { Resources = [] };

        // Act
        var result = await model.OnPostAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesAbsentScope()
    {
        // Arrange
        var existing = new ApiResourceScope { Id = ExistingEntityId, Scope = ExistingScopeName, ApiResourceId = ExistingEntityId };
        var resource = new ApiResource { Id = ExistingEntityId, Name = Generated.NewApiResourceName(), Scopes = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new Scopes(ctx.Object) { Resources = [] };

        // Act
        await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.Empty(resource.Scopes);
    }

    [Fact]
    public async Task OnPostAsync_ExistingScopePosted_UpdatesItsScopeAndSaves()
    {
        // Arrange
        var existing = new ApiResourceScope { Id = ExistingEntityId, Scope = ExistingScopeName, ApiResourceId = ExistingEntityId };
        var resource = new ApiResource { Id = ExistingEntityId, Name = Generated.NewApiResourceName(), Scopes = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new Scopes(ctx.Object) { Resources = [new ApiResourceScope { Id = ExistingEntityId, Scope = PostedScopeName }] };

        // Act
        await model.OnPostAsync(ExistingEntityId);

        // Assert
        var onlyScope = Assert.Single(resource.Scopes);
        Assert.Same(existing, onlyScope);
        Assert.Equal(PostedScopeName, onlyScope.Scope);
        ctx.Verify(c => c.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_PostedScopeMatchingNothingPersisted_ChangesNothing()
    {
        // Arrange
        var existing = new ApiResourceScope { Id = ExistingEntityId, Scope = ExistingScopeName, ApiResourceId = ExistingEntityId };
        var resource = new ApiResource { Id = ExistingEntityId, Name = Generated.NewApiResourceName(), Scopes = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new Scopes(ctx.Object)
        {
            Resources =
            [
                new ApiResourceScope { Id = ExistingEntityId, Scope = ExistingScopeName },
                new ApiResourceScope { Id = MissingEntityId, Scope = PostedScopeName },
            ]
        };

        // Act
        await model.OnPostAsync(ExistingEntityId);

        // Assert
        var onlyScope = Assert.Single(resource.Scopes);
        Assert.Same(existing, onlyScope);
        Assert.Equal(ExistingScopeName, onlyScope.Scope);
    }

    [Fact]
    public void Constructor_StartsWithNoResources()
    {
        // Arrange
        var ctx = new Mock<IConfigurationDbContext>();

        // Act
        var model = new Scopes(ctx.Object);

        // Assert
        Assert.Empty(model.Resources);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        // Arrange
        var resource = new ApiResource { Id = ExistingEntityId, Name = Generated.NewApiResourceName() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        var model = new Scopes(ctx.Object) { Resources = [] };

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
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        var model = new Scopes(ctx.Object) { Resources = [] };

        // Act
        var result = await model.OnPostAddRowAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        // Arrange
        var resource = new ApiResource { Id = ExistingEntityId, Name = Generated.NewApiResourceName() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        var model = new Scopes(ctx.Object) { Resources = [new ApiResourceScope { Id = ExistingEntityId, Scope = ExistingScopeName }] };

        // Act
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, 0);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Resources);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_NegativeIndex_LeavesRowsUnchanged()
    {
        // Arrange
        var resource = new ApiResource { Id = ExistingEntityId, Name = Generated.NewApiResourceName() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        var row = new ApiResourceScope { Id = ExistingEntityId, Scope = ExistingScopeName };
        var model = new Scopes(ctx.Object) { Resources = [row] };

        // Act
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, -1);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Same(row, Assert.Single(model.Resources));
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_IndexEqualToRowCount_LeavesRowsUnchanged()
    {
        // Arrange
        var resource = new ApiResource { Id = ExistingEntityId, Name = Generated.NewApiResourceName() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        var row = new ApiResourceScope { Id = ExistingEntityId, Scope = ExistingScopeName };
        var model = new Scopes(ctx.Object) { Resources = [row] };
        var rowCount = model.Resources.Count;

        // Act
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, rowCount);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Same(row, Assert.Single(model.Resources));
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        var model = new Scopes(ctx.Object) { Resources = [] };

        // Act
        var result = await model.OnPostRemoveRowAsync(MissingEntityId, 0);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
