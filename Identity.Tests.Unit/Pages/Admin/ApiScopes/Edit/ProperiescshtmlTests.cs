namespace Identity.Tests.Unit.Pages.Admin.ApiScopes.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.ApiScopes.Edit;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ProperiescshtmlTests
{
    private static readonly string PropertyKey = TestValues.NewPropertyKey();

    private static readonly string PropertyValue = TestValues.NewPropertyValue();

    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var scope = new ApiScope { Id = ExistingEntityId, Name = TestValues.NewApiResourceName(), Properties = [new ApiScopeProperty { Key = PropertyKey, Value = PropertyValue }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([scope]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        var model = new PropertiesModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.Properties);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiScope>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        var model = new PropertiesModel(ctx.Object);

        // Act
        var result = await model.OnGetAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsNewProperty()
    {
        // Arrange
        var scope = new ApiScope { Id = ExistingEntityId, Name = TestValues.NewApiResourceName(), Properties = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([scope]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new PropertiesModel(ctx.Object)
        {
            Properties = [new ApiScopeProperty { Id = 0, Key = PropertyKey, Value = PropertyValue }],
        };

        // Act
        var result = await model.OnPostAsync(ExistingEntityId);

        // Assert
        var onlyProperty = Assert.Single(scope.Properties);
        Assert.Equal(PropertyKey, onlyProperty.Key);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PropertiesModel.DetailsPageName, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiScope>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        var model = new PropertiesModel(ctx.Object) { Properties = [] };

        // Act
        var result = await model.OnPostAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_RemovesAbsentProperty()
    {
        // Arrange
        var existing = new ApiScopeProperty { Id = ExistingEntityId, Key = PropertyKey, Value = PropertyValue, ScopeId = ExistingEntityId };
        var scope = new ApiScope { Id = ExistingEntityId, Name = TestValues.NewApiResourceName(), Properties = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([scope]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);
        var model = new PropertiesModel(ctx.Object) { Properties = [] };

        // Act
        await model.OnPostAsync(ExistingEntityId);

        // Assert
        Assert.Empty(scope.Properties);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        // Arrange
        var scope = new ApiScope { Id = ExistingEntityId, Name = TestValues.NewApiResourceName() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([scope]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        var model = new PropertiesModel(ctx.Object) { Properties = [] };

        // Act
        var result = await model.OnPostAddRowAsync(ExistingEntityId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.Properties);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiScope>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        var model = new PropertiesModel(ctx.Object) { Properties = [] };

        // Act
        var result = await model.OnPostAddRowAsync(MissingEntityId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        // Arrange
        var scope = new ApiScope { Id = ExistingEntityId, Name = TestValues.NewApiResourceName() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([scope]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        var model = new PropertiesModel(ctx.Object) { Properties = [new ApiScopeProperty { Id = ExistingEntityId, Key = PropertyKey, Value = PropertyValue }] };

        // Act
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, 0);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Properties);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiScope>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        var model = new PropertiesModel(ctx.Object) { Properties = [] };

        // Act
        var result = await model.OnPostRemoveRowAsync(MissingEntityId, 0);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}