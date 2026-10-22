namespace Identity.Tests.Unit.Pages.Account.Manage;

using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ManageNavPagesNameTests
{
    [Fact]
    public void Grants_IsTheGrantsPageName()
    {
        // Act
        var page = ManageNavPages.Grants;

        // Assert
        Assert.Equal(nameof(Grants), page);
    }

    [Fact]
    public void Passkeys_IsThePasskeysPageName()
    {
        // Act
        var page = ManageNavPages.Passkeys;

        // Assert
        Assert.Equal(nameof(Passkeys), page);
    }

    [Fact]
    public void ExternalLogins_IsTheExternalLoginsPageName()
    {
        // Act
        var page = ManageNavPages.ExternalLogins;

        // Assert
        Assert.Equal(nameof(ExternalLogins), page);
    }
}
