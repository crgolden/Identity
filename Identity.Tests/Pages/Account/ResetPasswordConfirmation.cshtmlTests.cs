namespace Identity.Tests.Pages.Account;

using Identity.Pages.Account;

/// <summary>
/// Tests for ResetPasswordConfirmationModel.OnGet method.
/// </summary>
public class ResetPasswordConfirmationModelTests
{
    /// <summary>
    /// Verifies that invoking OnGet does not throw any exception.
    /// Condition: A newly constructed ResetPasswordConfirmationModel instance.
    /// Expected: No exception is thrown.
    /// </summary>
    [Fact]
    public void OnGet_WhenInvoked_DoesNotThrow()
    {
        // Arrange
        var model = new ResetPasswordConfirmationModel();

        // Act
        var ex = Record.Exception(() => model.OnGet());

        // Assert
        Assert.Null(ex);
    }

    /// <summary>
    /// Verifies that invoking OnGet does not alter the default ModelState validity or entries.
    /// Condition: A newly constructed ResetPasswordConfirmationModel instance with default ModelState.
    /// Expected: ModelState.IsValid remains true and entry count remains unchanged (0).
    /// </summary>
    [Fact]
    public void OnGet_WhenInvoked_ModelStateRemainsValidAndUnchanged()
    {
        // Arrange
        var model = new ResetPasswordConfirmationModel();
        var beforeIsValid = model.ModelState.IsValid;
        var beforeCount = model.ModelState.Count;

        // Act
        model.OnGet();

        // Assert
        Assert.True(beforeIsValid);
        Assert.Equal(0, beforeCount);
        Assert.True(model.ModelState.IsValid);
        Assert.Equal(beforeCount, model.ModelState.Count);
    }
}