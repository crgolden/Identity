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
}
