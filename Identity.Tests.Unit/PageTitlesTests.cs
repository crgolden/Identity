namespace Identity.Tests.Unit;

using Identity.Tests.Unit.Infrastructure;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class PageTitlesTests
{
    [Fact]
    public void Document_BlankPageTitle_IsTheApplicationName()
    {
        // Act
        var document = PageTitles.Document(Generated.NewBlank());

        // Assert
        Assert.Equal(PageTitles.Application, document);
    }

    [Fact]
    public void Document_NullPageTitle_IsTheApplicationName()
    {
        // Act
        var document = PageTitles.Document(null);

        // Assert
        Assert.Equal(PageTitles.Application, document);
    }

    [Fact]
    public void Document_PageTitle_PrefixesTheApplicationName()
    {
        // Arrange
        var pageTitle = Generated.NewDisplayName();

        // Act
        var document = PageTitles.Document(pageTitle);

        // Assert
        Assert.Equal($"{pageTitle} - {PageTitles.Application}", document);
    }
}
