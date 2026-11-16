namespace Identity.Tests.Unit.Pages;

using Identity.Pages;
using Identity.Tests.Unit.Infrastructure;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class UriValueTests
{
    [Fact]
    public void From_Null_ReturnsNull()
    {
        // Act
        var uri = UriValue.From(null);

        // Assert
        Assert.Null(uri);
    }

    [Fact]
    public void From_LocalPath_ReturnsARelativeUriKeepingTheText()
    {
        // Arrange
        var path = Generated.NewLocalPath();

        // Act
        var uri = UriValue.From(path);

        // Assert
        Assert.NotNull(uri);
        Assert.False(uri.IsAbsoluteUri);
        Assert.Equal(path, uri.OriginalString);
    }

    [Fact]
    public void From_Origin_ReturnsAnAbsoluteUriKeepingTheText()
    {
        // Arrange
        var origin = Generated.NewOrigin();

        // Act
        var uri = UriValue.From(origin);

        // Assert
        Assert.NotNull(uri);
        Assert.True(uri.IsAbsoluteUri);
        Assert.Equal(origin, uri.OriginalString);
    }
}
