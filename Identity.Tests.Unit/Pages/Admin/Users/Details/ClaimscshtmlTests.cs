namespace Identity.Tests.Unit.Pages.Admin.Users.Details;

using System.Security.Claims;
using Identity.Pages.Admin.Users.Details;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ClaimscshtmlTests
{
    private static readonly string ExistingUserId = TestValues.NewUserId().ToString();
    private static readonly string MissingUserId = TestValues.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([new Claim("role", "Admin")]);

        var model = new ClaimsModel(um.Object);
        var result = await model.OnGetAsync(ExistingUserId);

        Assert.IsType<PageResult>(result);
        var onlyClaim = Assert.Single(model.Claims);
        Assert.Equal("role", onlyClaim.Type);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);

        Assert.IsType<NotFoundResult>(await new ClaimsModel(um.Object).OnGetAsync(MissingUserId));
    }
}