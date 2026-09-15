namespace Identity.Tests.Unit.Pages.Admin.Users.Edit;

using System.Security.Claims;
using Identity.Pages.Admin.Users.Edit;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ClaimscshtmlTests
{
    private static readonly string ClaimType = TestValues.NewClaimType();

    private static readonly string ClaimValue = TestValues.NewClaimValue();

    private static readonly string RemovedClaimType = TestValues.NewClaimType();

    private static readonly string RemovedClaimValue = TestValues.NewClaimValue();

    private static readonly string ExistingUserId = TestValues.NewUserId().ToString();
    private static readonly string MissingUserId = TestValues.NewUserId().ToString();

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WhenFound()
    {
        // Arrange
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([new Claim(ClaimType, ClaimValue)]);
        var model = new ClaimsModel(um.Object);

        // Act
        var result = await model.OnGetAsync(ExistingUserId);

        // Assert
        Assert.IsType<PageResult>(result);
        var onlyClaim = Assert.Single(model.Claims);
        Assert.Equal(ClaimType, onlyClaim.Type);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);

        // Act
        var result = await new ClaimsModel(um.Object).OnGetAsync(MissingUserId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_ReplacesClaims_WhenFound()
    {
        // Arrange
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([new Claim(RemovedClaimType, RemovedClaimValue)]);
        um.Setup(m => m.RemoveClaimsAsync(user, It.IsAny<IEnumerable<Claim>>())).ReturnsAsync(IdentityResult.Success);
        um.Setup(m => m.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>())).ReturnsAsync(IdentityResult.Success);
        var model = new ClaimsModel(um.Object)
        {
            Claims = [new ClaimsModel.ClaimInputModel { Type = ClaimType, Value = ClaimValue }],
        };

        // Act
        var result = await model.OnPostAsync(ExistingUserId);

        // Assert
        um.Verify(m => m.RemoveClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()), Times.Once);
        um.Verify(m => m.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(ClaimsModel.DetailsPageName, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_RowWasAddedButNeverFilledIn_DropsItAndKeepsTheRest()
    {
        // Arrange
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([]);
        um.Setup(m => m.RemoveClaimsAsync(user, It.IsAny<IEnumerable<Claim>>())).ReturnsAsync(IdentityResult.Success);
        var saved = new List<Claim>();
        um.Setup(m => m.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()))
            .Callback<IdentityUser<Guid>, IEnumerable<Claim>>((_, claims) => saved.AddRange(claims))
            .ReturnsAsync(IdentityResult.Success);

        var model = new ClaimsModel(um.Object)
        {
            Claims =
            [
                new ClaimsModel.ClaimInputModel { Type = ClaimType, Value = ClaimValue },
                new ClaimsModel.ClaimInputModel(),
            ],
        };

        // Act
        await model.OnPostAsync(ExistingUserId);

        // Assert
        var onlySaved = Assert.Single(saved);
        Assert.Equal(ClaimType, onlySaved.Type);
        Assert.Equal(ClaimValue, onlySaved.Value);
    }

    [Fact]
    public async Task OnPostAsync_EveryRowIsBlank_SavesNothing()
    {
        // Arrange
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        um.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync([]);
        um.Setup(m => m.RemoveClaimsAsync(user, It.IsAny<IEnumerable<Claim>>())).ReturnsAsync(IdentityResult.Success);
        var model = new ClaimsModel(um.Object) { Claims = [new ClaimsModel.ClaimInputModel()] };

        // Act
        var result = await model.OnPostAsync(ExistingUserId);

        // Assert
        um.Verify(m => m.AddClaimsAsync(user, It.IsAny<IEnumerable<Claim>>()), Times.Never);
        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);
        var model = new ClaimsModel(um.Object) { Claims = [] };

        // Act
        var result = await model.OnPostAsync(MissingUserId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAddRowAsync_AddsBlankRow_WhenFound()
    {
        // Arrange
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        var model = new ClaimsModel(um.Object) { Claims = [] };

        // Act
        var result = await model.OnPostAddRowAsync(ExistingUserId);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Single(model.Claims);
    }

    [Fact]
    public async Task OnPostAddRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);
        var model = new ClaimsModel(um.Object) { Claims = [] };

        // Act
        var result = await model.OnPostAddRowAsync(MissingUserId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_RemovesRow_WhenValidIndex()
    {
        // Arrange
        var user = new IdentityUser<Guid> { UserName = TestValues.NewUserName() };
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(ExistingUserId)).ReturnsAsync(user);
        var model = new ClaimsModel(um.Object) { Claims = [new ClaimsModel.ClaimInputModel { Type = ClaimType, Value = ClaimValue }] };

        // Act
        var result = await model.OnPostRemoveRowAsync(ExistingUserId, 0);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Claims);
    }

    [Fact]
    public async Task OnPostRemoveRowAsync_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.FindByIdAsync(MissingUserId)).ReturnsAsync((IdentityUser<Guid>?)null);
        var model = new ClaimsModel(um.Object) { Claims = [] };

        // Act
        var result = await model.OnPostRemoveRowAsync(MissingUserId, 0);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}