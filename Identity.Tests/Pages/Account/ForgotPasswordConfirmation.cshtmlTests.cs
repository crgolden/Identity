namespace Identity.Tests.Pages.Account;

using Identity.Pages.Account;

/// <summary>
/// Tests for ForgotPasswordConfirmation PageModel.
/// </summary>
public class ForgotPasswordConfirmationTests
{
    /// <summary>
    /// Ensures that calling OnGet does not modify ModelState error count.
    /// Input conditions: a new instance of ForgotPasswordConfirmation with a specified
    /// number of ModelState errors (0, 1, or 5).
    /// Expected result: OnGet completes without throwing and the ModelState.ErrorCount
    /// remains the same after invocation.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void OnGet_ModelStateUnchanged_AfterExecution(int initialErrorCount)
    {
        // Arrange
        var page = new ForgotPasswordConfirmation();
        for (int i = 0; i < initialErrorCount; i++)
        {
            page.ModelState.AddModelError($"key{i}", "error");
        }

        int beforeErrorCount = page.ModelState.ErrorCount;

        // Act
        page.OnGet();

        // Assert
        Assert.Equal(beforeErrorCount, page.ModelState.ErrorCount);
    }

    /// <summary>
    /// Verifies that OnGet can be invoked multiple times without throwing and is idempotent
    /// with respect to ModelState error count.
    /// Input conditions: a new instance of ForgotPasswordConfirmation with one ModelState error.
    /// Expected result: multiple calls to OnGet do not throw and do not change ModelState.ErrorCount.
    /// </summary>
    [Fact]
    public void OnGet_CanBeCalledMultipleTimes_NoExceptionAndIdempotent()
    {
        // Arrange
        var page = new ForgotPasswordConfirmation();
        page.ModelState.AddModelError("k", "err");
        int before = page.ModelState.ErrorCount;

        // Act
        page.OnGet();
        page.OnGet();

        // Assert
        Assert.Equal(before, page.ModelState.ErrorCount);
    }
}