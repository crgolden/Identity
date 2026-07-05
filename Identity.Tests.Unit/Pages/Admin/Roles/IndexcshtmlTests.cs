namespace Identity.Tests.Unit.Pages.Admin.Roles;

using Identity.Pages.Admin.Roles;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class IndexcshtmlTests
{
    [Fact]
    public void IsPageModel()
    {
        Assert.IsAssignableFrom<PageModel>(new IndexModel(MockHelpers.MockRoleManager().Object));
    }

    [Fact]
    public async Task OnGetAsync_ReturnsSortedByName()
    {
        var rm = MockHelpers.MockRoleManager();
        var mockSet = MockDbSetHelper.BuildMockDbSet(new[] { new IdentityRole<Guid>("z-role"), new IdentityRole<Guid>("a-role") });
        rm.Setup(m => m.Roles).Returns(mockSet.Object);

        var model = new IndexModel(rm.Object);
        await model.OnGetAsync();

        Assert.Equal(2, model.Roles.Count);
        Assert.Equal("a-role", model.Roles[0].Name);
    }
}
