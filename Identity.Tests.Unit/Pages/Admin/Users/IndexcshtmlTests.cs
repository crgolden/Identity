namespace Identity.Tests.Unit.Pages.Admin.Users;

using Identity.Pages.Admin.Users;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    private static readonly string FirstUserNameAlphabetically =
        TestValues.NewTokenFromFirstHalfOfAlphabet(9);

    private static readonly string LastUserNameAlphabetically =
        TestValues.NewTokenFromSecondHalfOfAlphabet(9);

    [Fact]
    public void IsPageModel()
    {
        var um = MockHelpers.MockUserManager();
        Assert.IsType<PageModel>(new IndexModel(um.Object), exactMatch: false);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsSortedByUsername()
    {
        var data = new[]
        {
            new IdentityUser<Guid> { UserName = LastUserNameAlphabetically },
            new IdentityUser<Guid> { UserName = FirstUserNameAlphabetically },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.Users).Returns(mockSet.Object);

        var model = new IndexModel(um.Object);
        await model.OnGetAsync();

        Assert.Equal(data.Length, model.Users.Count);
        Assert.Equal(FirstUserNameAlphabetically, model.Users[0].UserName);
    }
}