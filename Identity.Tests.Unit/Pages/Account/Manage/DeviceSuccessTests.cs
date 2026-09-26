namespace Identity.Tests.Unit.Pages.Account.Manage;

using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DeviceSuccessTests
{
    [Fact]
    public void Constructor_NoParameters_DoesNotThrow()
    {
        // Act
        var model = new DeviceSuccess();

        // Assert
        Assert.NotNull(model);
    }

    [Fact]
    public void Constructor_IsPageModel()
    {
        // Act
        var model = new DeviceSuccess();

        // Assert
        Assert.IsType<PageModel>(model, exactMatch: false);
    }
}
