namespace Identity.Tests.Unit.Pages.Admin.Roles;

using Identity.Pages.Admin.Roles;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    private static readonly string FirstRoleName = TestValues.NewFirstAlphabeticalName();

    private static readonly string LastRoleName = TestValues.NewLastAlphabeticalName();

    [Fact]
    public void IsPageModel()
    {
        // Arrange
        var rm = MockHelpers.MockRoleManager();

        // Act
        var model = new IndexModel(rm.Object);

        // Assert
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsSortedByName()
    {
        // Arrange
        var rm = MockHelpers.MockRoleManager();
        var data = new[] { new IdentityRole<Guid>(LastRoleName), new IdentityRole<Guid>(FirstRoleName) };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        rm.Setup(m => m.Roles).Returns(mockSet.Object);
        var model = new IndexModel(rm.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(data.Length, model.Roles.Count);
        Assert.Equal(FirstRoleName, model.Roles[0].Name);
    }
}