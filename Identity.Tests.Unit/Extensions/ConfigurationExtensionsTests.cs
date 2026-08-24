namespace Identity.Tests.Unit.Extensions;

using Identity.Extensions;
using Infrastructure;
using Microsoft.Extensions.Configuration;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class ConfigurationExtensionsTests
{
    [Fact]
    public void GetRequired_ReturnsTheValueForAPresentStringKey()
    {
        // Arrange
        var googleClientIdKey = Guid.NewGuid().ToString();
        var googleClientId = Guid.NewGuid().ToString();
        var configuration = ConfigurationWith(googleClientIdKey, googleClientId);

        // Act
        var result = configuration.GetRequired<string>(googleClientIdKey);

        // Assert
        Assert.Equal(googleClientId, result);
    }

    [Fact]
    public void GetRequired_ConvertsAPresentKeyToTheRequestedNonStringType()
    {
        // Arrange
        var elasticsearchNodeKey = Guid.NewGuid().ToString();
        var elasticsearchNode = new Uri($"https://example.com/{Guid.NewGuid():N}");
        var configuration = ConfigurationWith(elasticsearchNodeKey, elasticsearchNode.AbsoluteUri);

        // Act
        var result = configuration.GetRequired<Uri>(elasticsearchNodeKey);

        // Assert
        Assert.IsType<Uri>(result);
        Assert.Equal(elasticsearchNode, result);
    }

    [Fact]
    public void GetRequired_ThrowsNamingTheKeyWhenItIsAbsent()
    {
        // Arrange
        var configuredKey = Guid.NewGuid().ToString();
        var absentKey = Guid.NewGuid().ToString();
        var configuration = ConfigurationWith(configuredKey, Guid.NewGuid().ToString());

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => configuration.GetRequired<string>(absentKey));

        // Assert
        Assert.Contains(absentKey, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(configuredKey, exception.Message, StringComparison.Ordinal);
    }

    private static IConfiguration ConfigurationWith(string key, string value)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })
            .Build();
    }
}
