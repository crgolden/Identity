#pragma warning disable CS8604
#pragma warning disable CS8625
namespace Identity.Tests.Pages.Account.Manage;

using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Unit tests for <see cref="Identity.Pages.Account.Manage.DeviceSuccessModel"/>.</summary>
[Trait("Category", "Unit")]
public class DeviceSuccessModelTests
{
    /// <summary>
    /// Verifies that DeviceSuccessModel can be constructed without parameters and does not throw.
    /// Inputs: no constructor arguments.
    /// Expected: no exception is thrown and the constructed instance is not null.
    /// </summary>
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

    /// <summary>
    /// Verifies that DeviceSuccessModel is a PageModel instance.
    /// Inputs: a freshly constructed DeviceSuccessModel.
    /// Expected: the instance is assignable to PageModel.
    /// </summary>
    [Fact]
    public void Constructor_IsPageModel()
    {
        // Arrange & Act
        var model = new DeviceSuccessModel();

        // Assert
        Assert.IsType<PageModel>(model, exactMatch: false);
    }
}
