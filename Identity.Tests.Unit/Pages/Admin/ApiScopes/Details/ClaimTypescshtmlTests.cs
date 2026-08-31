namespace Identity.Tests.Unit.Pages.Admin.ApiScopes.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Identity.Pages.Admin.ApiScopes.Details;
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
        var scope = new ApiScope { Id = ExistingEntityId, Name = "api1", UserClaims = [new ApiScopeClaim { Type = "sub" }] };
        var mockSet = MockDbSetHelper.BuildMockDbSet([scope]);
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        var model = new ClaimTypesModel(ctx.Object);
        var result = await model.OnGetAsync(ExistingEntityId);
        Assert.IsType<PageResult>(result);
        Assert.Single(model.Scope.UserClaims);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var mockSet = MockDbSetHelper.BuildMockDbSet(Array.Empty<ApiScope>());
        var ctx = new Mock<IConfigurationDbContext>();
        ctx.Setup(c => c.ApiScopes).Returns(mockSet.Object);
        Assert.IsType<NotFoundResult>(await new ClaimTypesModel(ctx.Object).OnGetAsync(MissingEntityId));
    }
}