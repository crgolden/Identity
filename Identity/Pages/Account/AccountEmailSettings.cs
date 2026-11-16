namespace Identity.Pages.Account;

using System.Globalization;
using System.Text.Encodings.Web;
using Identity.Extensions;

public sealed class AccountEmailSettings
{
    public AccountEmailSettings(string sender, string confirmAccountHtmlFormat, string resetPasswordHtmlFormat)
    {
        Sender = sender;
        ConfirmAccountHtmlFormat = confirmAccountHtmlFormat;
        ResetPasswordHtmlFormat = resetPasswordHtmlFormat;
    }

    public string Sender { get; }

    public string ConfirmAccountHtmlFormat { get; }

    public string ResetPasswordHtmlFormat { get; }

    public static AccountEmailSettings Read(IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(nameof(AccountEmailSettings));
        return new AccountEmailSettings(
            section.GetRequired<string>(nameof(Sender)),
            section.GetRequired<string>(nameof(ConfirmAccountHtmlFormat)),
            section.GetRequired<string>(nameof(ResetPasswordHtmlFormat)));
    }

    public string ConfirmAccountHtml(Uri callbackUrl)
    {
        ThrowIfNull(callbackUrl);
        return Format(CultureInfo.InvariantCulture, ConfirmAccountHtmlFormat, HtmlEncoder.Default.Encode(callbackUrl.OriginalString));
    }

    public string ResetPasswordHtml(Uri callbackUrl)
    {
        ThrowIfNull(callbackUrl);
        return Format(CultureInfo.InvariantCulture, ResetPasswordHtmlFormat, HtmlEncoder.Default.Encode(callbackUrl.OriginalString));
    }
}
