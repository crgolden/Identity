#pragma warning disable CS8604
#pragma warning disable CS8625
namespace Identity.Tests.Pages.Account.Manage;
using Infrastructure;

using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DeviceSuccessModelTests
{
    [Fact]
    public void Constructor_NoParameters_DoesNotThrow()
    {
        // Arrange & Act
        DeviceSuccessModel model = null!;
        var ex = Record.Exception(() => model = new DeviceSuccessModel());

        // Assert
        Assert.Null(ex);
        Assert.NotNull(model);
    }

    [Fact]
    public void Constructor_IsPageModel()
    {
        // Arrange & Act
        var model = new DeviceSuccessModel();

        // Assert
        Assert.IsType<PageModel>(model, exactMatch: false);
    }
}
