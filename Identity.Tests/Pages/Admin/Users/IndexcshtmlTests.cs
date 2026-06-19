namespace Identity.Tests.Pages.Admin.Users;

using Identity.Pages.Admin.Users;
using Identity.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    [Fact]
    public void IsPageModel()
    {
        var um = MockHelpers.MockUserManager();
        Assert.IsAssignableFrom<PageModel>(new IndexModel(um.Object));
    }

    [Fact]
    public async Task OnGetAsync_ReturnsSortedByUsername()
    {
        var data = new[]
        {
            new IdentityUser<Guid> { UserName = "z-user" },
            new IdentityUser<Guid> { UserName = "a-user" },
        };
        var mockSet = MockDbSetHelper.BuildMockDbSet(data);
        var um = MockHelpers.MockUserManager();
        um.Setup(m => m.Users).Returns(mockSet.Object);

        var model = new IndexModel(um.Object);
        await model.OnGetAsync();

        Assert.Equal(2, model.Users.Count);
        Assert.Equal("a-user", model.Users[0].UserName);
    }
}
