#pragma warning disable CS8604
#pragma warning disable CS8625
namespace Identity.Tests.Pages.Device;

using Identity.Pages.Device;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Unit tests for <see cref="Identity.Pages.Device.SuccessModel"/>.</summary>
[Trait("Category", "Unit")]
public class DeviceSuccessModelTests
{
    /// <summary>
    /// Verifies that SuccessModel can be constructed without parameters and does not throw.
    /// Inputs: no constructor arguments.
    /// Expected: no exception is thrown and the constructed instance is not null.
    /// </summary>
    [Fact]
    public void Constructor_NoParameters_DoesNotThrow()
    {
        // Arrange & Act
        SuccessModel model = null!;
        var ex = Record.Exception(() => model = new SuccessModel());

        // Assert
        Assert.Null(ex);
        Assert.NotNull(model);
    }

    /// <summary>
    /// Verifies that SuccessModel is a PageModel instance.
    /// Inputs: a freshly constructed SuccessModel.
    /// Expected: the instance is assignable to PageModel.
    /// </summary>
    [Fact]
    public void Constructor_IsPageModel()
    {
        // Arrange & Act
        var model = new SuccessModel();

        // Assert
        Assert.IsType<PageModel>(model, exactMatch: false);
    }
}
