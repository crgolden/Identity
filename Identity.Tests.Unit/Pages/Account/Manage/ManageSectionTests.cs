namespace Identity.Tests.Unit.Pages.Account.Manage;

using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ManageSectionTests
{
    [Fact]
    public void With_NewPage_DerivesAspPageFromTheNewPage()
    {
        // Arrange
        var section = new ManageSection(Generated.NewPathSegment(), Generated.NewDisplayName(), Generated.NewPageName(), RequiresExternalLogins: true);
        var movedPage = Generated.NewDifferentPageName();
        var constructedAtMovedPage = new ManageSection(section.NavId, section.Title, movedPage, section.RequiresExternalLogins);

        // Act
        var moved = section with { Page = movedPage };

        // Assert
        Assert.Equal(constructedAtMovedPage.AspPage, moved.AspPage);
    }

    [Fact]
    public void AspPage_IsThePageRelativeToTheCurrentFolder()
    {
        // Arrange
        var page = Generated.NewPageName();

        // Act
        var section = new ManageSection(Generated.NewPathSegment(), Generated.NewDisplayName(), page, RequiresExternalLogins: false);

        // Assert
        Assert.Equal($"./{page}", section.AspPage);
    }

    [Fact]
    public void LinkId_IsThePrefixPlusTheNavId()
    {
        // Arrange
        var navId = Generated.NewPathSegment();

        // Act
        var section = new ManageSection(navId, Generated.NewDisplayName(), Generated.NewPageName(), RequiresExternalLogins: false);

        // Assert
        Assert.Equal(ManageSection.LinkIdPrefix + navId, section.LinkId);
    }
}
