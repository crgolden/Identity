namespace Identity.Tests.Unit.Logging;

using System.Reflection;
using System.Text;
using global::Identity.Logging;
using Identity.Tests.Unit.Infrastructure;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class DuendeLicensingVocabularyTests
{
    public static TheoryData<string> DroppedEventNames => new(DuendeLicenseNotice.NoLicenseConfiguredEventNames);

    [Fact]
    public void TheShippedLicensingAssembly_StillDefinesTheFilteredLoggerCategory()
    {
        // Arrange
        var licensing = Assembly.Load(DuendeLicenseEventConstants.LicensingAssemblyName);

        // Act
        var validator = licensing.GetType(DuendeLicenseNotice.LicenseValidatorSourceContext, throwOnError: false);

        // Assert
        Assert.NotNull(validator);
    }

    [Theory]
    [MemberData(nameof(DroppedEventNames))]
    public void TheShippedLicensingAssembly_StillLogsEveryDroppedEventName(string eventName)
    {
        // Arrange
        var licensing = Assembly.Load(DuendeLicenseEventConstants.LicensingAssemblyName);
        var eventNameBytes = Encoding.Unicode.GetBytes(eventName);

        // Act
        var position = File.ReadAllBytes(licensing.Location).AsSpan().IndexOf(eventNameBytes);

        // Assert
        Assert.True(position >= 0, $"{licensing.Location} no longer carries the event name '{eventName}' the license-notice filter drops.");
    }
}
