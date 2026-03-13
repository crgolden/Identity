namespace Identity.Tests.Pages.Account;

using Identity.Pages.Account;

/// <summary>
/// Tests for Identity.Pages.Account.LockoutModel.
/// </summary>
public class LockoutModelTests
{
    /// <summary>
    /// Verifies that calling OnGet leaves the PageModel's ModelState validity unchanged.
    /// 
    /// Conditions:
    /// - addError: if true, a model error is added before calling OnGet.
    /// - callCount: the number of times OnGet is invoked.
    /// 
    /// Expected:
    /// - The ModelState.IsValid property after calling OnGet(s) matches its initial value,
    ///   i.e., OnGet does not modify ModelState validity and does not throw.
    /// </summary>
    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 3)]
    [InlineData(true, 1)]
    [InlineData(true, 3)]
    public void OnGet_ModelStateInitialState_RemainsUnchanged(bool addError, int callCount)
    {
        // Arrange
        var model = new LockoutModel();

        if (addError)
        {
            model.ModelState.AddModelError("testKey", "test error");
        }

        var initialIsValid = model.ModelState.IsValid;

        // Act
        Exception? caught = null;
        try
        {
            for (var i = 0; i < callCount; i++)
            {
                model.OnGet();
            }
        }
        catch (Exception ex)
        {
            caught = ex;
        }

        // Assert
        Assert.Null(caught); // Ensure no exception was thrown
        Assert.Equal(initialIsValid, model.ModelState.IsValid);
    }

    /// <summary>
    /// Ensures that calling OnGet repeatedly on a fresh LockoutModel does not throw and keeps ModelState valid.
    /// 
    /// Conditions:
    /// - Repeated calls simulate multiple GET requests.
    /// 
    /// Expected:
    /// - No exception thrown and ModelState remains valid (true) after multiple calls.
    /// </summary>
    [Fact]
    public void OnGet_RepeatedCallsOnFreshModel_DoesNotThrowAndModelStateIsValid()
    {
        // Arrange
        var model = new LockoutModel();
        // Precondition check
        Assert.True(model.ModelState.IsValid);

        // Act & Assert
        Exception? caught = null;
        try
        {
            model.OnGet();
            model.OnGet();
            model.OnGet();
        }
        catch (Exception ex)
        {
            caught = ex;
        }

        Assert.Null(caught);
        Assert.True(model.ModelState.IsValid);
    }
}