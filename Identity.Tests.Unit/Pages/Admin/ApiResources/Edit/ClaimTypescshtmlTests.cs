namespace Identity.Tests.Unit.Pages.Admin.ApiResources.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.ApiResources.Edit;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ClaimTypescshtmlTests
{
    private static readonly int ExistingEntityId = TestValues.NewEntityId();
    private static readonly int MissingEntityId = ExistingEntityId + 1;

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var resource = new ApiResource { Id = ExistingEntityId, Name = TestValues.NewApiResourceName(), UserClaims = [new ApiResourceClaim { Id = ExistingEntityId, Type = "sub" }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new ClaimTypesModel(ctx.Object);
        var result = await model.OnGetAsync(ExistingEntityId);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.ClaimTypes);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        Assert.IsType<NotFoundResult>(await new ClaimTypesModel(ctx.Object).OnGetAsync(MissingEntityId));
    }

    [Fact]
    public async Task OnPostAsync_AddsNewClaimType()
    {
        var resource = new ApiResource { Id = ExistingEntityId, Name = TestValues.NewApiResourceName(), UserClaims = [] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new ClaimTypesModel(ctx.Object) { ClaimTypes = [new ApiResourceClaim { Id = 0, Type = "email" }] };
        var result = await model.OnPostAsync(ExistingEntityId);

        var onlyUserClaim = Assert.Single(resource.UserClaims);
        Assert.Equal("email", onlyUserClaim.Type);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/ApiResources/Details/ClaimTypes", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new ClaimTypesModel(ctx.Object) { ClaimTypes = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostAsync(MissingEntityId));
    }

    [Fact]
    public async Task OnPostAsync_RemovesAbsentClaimType()
    {
        var existing = new ApiResourceClaim { Id = ExistingEntityId, Type = "sub", ApiResourceId = ExistingEntityId };
        var resource = new ApiResource { Id = ExistingEntityId, Name = TestValues.NewApiResourceName(), UserClaims = [existing] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);
        ctx.Setup(c => c.SaveChangesAsync()).ReturnsAsync(1);

        var model = new ClaimTypesModel(ctx.Object) { ClaimTypes = [] };
        await model.OnPostAsync(ExistingEntityId);

        Assert.Empty(resource.UserClaims);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        var resource = new ApiResource { Id = ExistingEntityId, Name = TestValues.NewApiResourceName() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new ClaimTypesModel(ctx.Object) { ClaimTypes = [] };
        var result = await model.OnPostAddRowAsync(ExistingEntityId);

        Assert.IsType<PageResult>(result);
        Assert.Single(model.ClaimTypes);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new ClaimTypesModel(ctx.Object) { ClaimTypes = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostAddRowAsync(MissingEntityId));
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        var resource = new ApiResource { Id = ExistingEntityId, Name = TestValues.NewApiResourceName() };
        var mockSet = MockDbSetHelper.BuildMockDbSet([resource]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new ClaimTypesModel(ctx.Object) { ClaimTypes = [new ApiResourceClaim { Id = ExistingEntityId, Type = "sub" }] };
        var result = await model.OnPostRemoveRowAsync(ExistingEntityId, 0);

        Assert.IsType<PageResult>(result);
        Assert.Empty(model.ClaimTypes);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiResource>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiResources).Returns(mockSet.Object);

        var model = new ClaimTypesModel(ctx.Object) { ClaimTypes = [] };
        Assert.IsType<NotFoundResult>(await model.OnPostRemoveRowAsync(MissingEntityId, 0));
    }
}