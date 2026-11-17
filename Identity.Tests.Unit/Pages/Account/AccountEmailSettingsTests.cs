namespace Identity.Tests.Unit.Pages.Account;

using System.Reflection;
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

    [Fact]
    public void ConfirmAccountHtml_NullCallbackUrl_ThrowsArgumentNullExceptionNamingIt()
    {
        // Arrange
        var settings = new AccountEmailSettings(Generated.NewEmailAddress(), Generated.NewDescription(), Generated.NewDescription());
        var builder = typeof(AccountEmailSettings).GetMethod(nameof(AccountEmailSettings.ConfirmAccountHtml));
        Assert.NotNull(builder);
        var callbackUrlParameter = Assert.Single(builder.GetParameters());

        // Act
        var thrown = Assert.Throws<TargetInvocationException>(() => builder.Invoke(settings, [null]));

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(thrown.InnerException);
        Assert.Equal(callbackUrlParameter.Name, argumentNull.ParamName);
    }

    [Fact]
    public void ResetPasswordHtml_NullCallbackUrl_ThrowsArgumentNullExceptionNamingIt()
    {
        // Arrange
        var settings = new AccountEmailSettings(Generated.NewEmailAddress(), Generated.NewDescription(), Generated.NewDescription());
        var builder = typeof(AccountEmailSettings).GetMethod(nameof(AccountEmailSettings.ResetPasswordHtml));
        Assert.NotNull(builder);
        var callbackUrlParameter = Assert.Single(builder.GetParameters());

        // Act
        var thrown = Assert.Throws<TargetInvocationException>(() => builder.Invoke(settings, [null]));

        // Assert
        var argumentNull = Assert.IsType<ArgumentNullException>(thrown.InnerException);
        Assert.Equal(callbackUrlParameter.Name, argumentNull.ParamName);
    }
}
