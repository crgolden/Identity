namespace Identity.Tests.Unit.Pages.Admin.Users;

using Identity.Pages.Admin.Users;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexTests
{
    private static readonly string FirstUserNameAlphabetically =
        Generated.NewFirstAlphabeticalName();

    private static readonly string LastUserNameAlphabetically =
        Generated.NewLastAlphabeticalName();

    [Fact]
    public void IsPageModel()
    {
        // Arrange
        var um = MockHelpers.MockUserManager();

        // Act
        var model = new Index(um.Object);

        // Assert
        Assert.IsType<PageModel>(model, exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsSortedByUsername()
    {
        // Arrange
        var data = new[]
        {
            new IdentityUser<Guid> { UserName = LastUserNameAlphabetically },
            new IdentityUser<Guid> { UserName = FirstUserNameAlphabetically },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.Users).Returns(mockSet.Object);
        var model = new Index(um.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(data.Length, model.Users.Count);
        Assert.Equal(FirstUserNameAlphabetically, model.Users[0].UserName);
    }
}
