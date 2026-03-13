using Identity.Pages;

namespace Identity.Tests.Pages;

/// <summary>
/// Tests for PrivacyModel located in Identity.Pages namespace.
/// </summary>
public class PrivacyModelTests
{
    /// <summary>
    /// Verifies that calling OnGet multiple times does not throw and that ModelState remains valid
    /// when there are no pre-existing model errors.
    /// Input conditions: numberOfInvocations indicates how many times OnGet is invoked (1, 3, 5).
    /// Expected result: No exception is thrown and ModelState.IsValid remains true.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void OnGet_MultipleInvocations_DoesNotThrowAndModelStateRemainsValid(int numberOfInvocations)
    {
        // Arrange
        var model = new PrivacyModel();
        Assert.True(model.ModelState.IsValid); // sanity check before act

        // Act
        Exception? caught = null;
        for (int i = 0; i < numberOfInvocations; i++)
        {
            caught = Record.Exception(() => model.OnGet());
            // quick check each iteration to surface immediate exceptions
            Assert.Null(caught);
        }

        // Assert
        Assert.Null(caught);
        Assert.True(model.ModelState.IsValid);
    }

    /// <summary>
    /// Verifies that OnGet preserves existing ModelState errors and does not clear them.
    /// Input conditions: numberOfInvocations indicates how many times OnGet is invoked; initialHasErrors = true.
    /// Expected result: No exception is thrown, the error count remains equal to the original, and IsValid remains false.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void OnGet_WithPreExistingModelStateErrors_PreservesErrorsAfterInvocations(int numberOfInvocations)
    {
        // Arrange
        var model = new PrivacyModel();
        // Introduce model state errors to simulate invalid model before OnGet
        model.ModelState.AddModelError("TestKey", "Test error message");
        var initialErrorCount = model.ModelState.ErrorCount;
        Assert.False(model.ModelState.IsValid);
        Assert.Equal(1, initialErrorCount);

        // Act
        Exception? caught = null;
        for (int i = 0; i < numberOfInvocations; i++)
        {
            caught = Record.Exception(() => model.OnGet());
            Assert.Null(caught);
        }

        // Assert
        Assert.Null(caught);
        Assert.False(model.ModelState.IsValid);
        Assert.Equal(initialErrorCount, model.ModelState.ErrorCount);
    }
}