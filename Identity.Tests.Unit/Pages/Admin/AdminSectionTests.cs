namespace Identity.Tests.Unit.Pages.Admin;

using Identity.Pages.Admin;
using Identity.Tests.Unit.Infrastructure;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class AdminSectionTests
{
    [Fact]
    public void With_NewPage_DerivesCardIdAndPathFromTheNewPage()
    {
        // Arrange
        var section = new AdminSection(Generated.NewDisplayName(), Generated.NewDisplayName(), Generated.NewAdminSectionPage());
        var movedPage = Generated.NewAdminSectionPage();
        var constructedAtMovedPage = new AdminSection(section.Title, section.Description, movedPage);

        // Act
        var moved = section with { Page = movedPage };

        // Assert
        Assert.Equal(constructedAtMovedPage.CardId, moved.CardId);
        Assert.Equal(constructedAtMovedPage.Path, moved.Path);
    }

    [Fact]
    public void CardId_IsThePrefixPlusTheLowercasedFolderOfThePage()
    {
        // Arrange
        var area = Generated.NewPathSegment();
        var folder = Generated.NewPageName();
        var uppercaseFolder = folder.ToUpperInvariant();
        var leaf = Generated.NewPathSegment();
        var expectedCardId = $"{AdminSection.CardIdPrefix}{folder}";
        var section = new AdminSection(Generated.NewDisplayName(), Generated.NewDisplayName(), $"/{area}/{uppercaseFolder}/{leaf}");

        // Act
        var cardId = section.CardId;

        // Assert
        Assert.Equal(expectedCardId, cardId);
    }

    [Fact]
    public void Path_IsThePageWithoutItsLastSegment()
    {
        // Arrange
        var area = Generated.NewPathSegment();
        var folder = Generated.NewPageName();
        var leaf = Generated.NewPathSegment();
        var expectedPath = $"/{area}/{folder}";
        var section = new AdminSection(Generated.NewDisplayName(), Generated.NewDisplayName(), $"{expectedPath}/{leaf}");

        // Act
        var path = section.Path;

        // Assert
        Assert.Equal(expectedPath, path);
    }
}
