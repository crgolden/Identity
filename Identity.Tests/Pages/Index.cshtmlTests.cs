using Identity.Pages;

namespace Identity.Tests.Pages
{
    /// <summary>
    /// Unit tests for Identity.Pages.IndexModel.
    /// </summary>
    public class IndexModelTests
    {
        /// <summary>
        /// Verifies that calling OnGet a variable number of times does not throw and does not modify ModelState.
        /// Input: times - number of times to invoke OnGet (including 0 to test default state without invocation).
        /// Expected: No exception is thrown; ModelState remains valid and empty after calls.
        /// </summary>
        /// <param name="times">Number of times to call OnGet.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        public void OnGet_RepeatedCalls_DoesNotThrowOrModifyModelState(int times)
        {
            // Arrange
            var model = new IndexModel();

            // Capture initial state
            var initialIsValid = model.ModelState.IsValid;
            var initialCount = model.ModelState.Count;

            // Act
            Exception? caught = null;
            try
            {
                for (int i = 0; i < times; i++)
                {
                    model.OnGet();
                }
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            // Assert
            Assert.Null(caught);
            Assert.Equal(initialIsValid, model.ModelState.IsValid);
            Assert.Equal(initialCount, model.ModelState.Count);
        }
    }
}