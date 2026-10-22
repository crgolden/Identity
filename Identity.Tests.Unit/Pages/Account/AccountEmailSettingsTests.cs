namespace Identity.Tests.Unit.Pages.Account;

using Identity.Pages.Account;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.Extensions.Configuration;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class AccountEmailSettingsTests
{
    [Fact]
    public void Read_ConfiguredSection_ReadsEverySetting()
    {
        // Arrange
        var sender = Generated.NewEmailAddress();
        var confirmAccountHtmlFormat = Generated.NewDescription();
        var resetPasswordHtmlFormat = Generated.NewDescription();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ConfigurationPath.Combine(nameof(AccountEmailSettings), nameof(AccountEmailSettings.Sender))] = sender,
                [ConfigurationPath.Combine(nameof(AccountEmailSettings), nameof(AccountEmailSettings.ConfirmAccountHtmlFormat))] = confirmAccountHtmlFormat,
                [ConfigurationPath.Combine(nameof(AccountEmailSettings), nameof(AccountEmailSettings.ResetPasswordHtmlFormat))] = resetPasswordHtmlFormat,
            })
            .Build();

        // Act
        var settings = AccountEmailSettings.Read(configuration);

        // Assert
        Assert.Equal(sender, settings.Sender);
        Assert.Equal(confirmAccountHtmlFormat, settings.ConfirmAccountHtmlFormat);
        Assert.Equal(resetPasswordHtmlFormat, settings.ResetPasswordHtmlFormat);
    }
}
