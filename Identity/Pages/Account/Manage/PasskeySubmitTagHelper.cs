namespace Identity.Pages.Account.Manage;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Razor.TagHelpers;
using static Microsoft.AspNetCore.Razor.TagHelpers.NullHtmlEncoder;

[HtmlTargetElement(TagName)]
public class PasskeySubmitTagHelper : TagHelper
{
    internal const string TagName = "passkey-submit";

    internal const string OperationAttributeName = "operation";

    internal const string NameAttributeName = "name";

    internal const string EmailNameAttributeName = "email-name";

    internal const string AutofillAttributeName = "autofill";

    internal const string RequestTokenNameAttributeName = "request-token-name";

    internal const string RequestTokenValueAttributeName = "request-token-value";

    internal const string AutofillOn = "on";

    internal const string AutofillOff = "off";

    internal const string ButtonOpeningTag = "<button type=\"submit\" name=\"__passkeySubmit\" ";

    internal const string ButtonClosingTag = "</button>";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAntiforgery _antiforgery;

    public PasskeySubmitTagHelper(IHttpContextAccessor httpContextAccessor, IAntiforgery antiforgery)
    {
        _httpContextAccessor = httpContextAccessor;
        _antiforgery = antiforgery;
    }

    [HtmlAttributeName(OperationAttributeName)]
    public PasskeyOperation? Operation { get; set; }

    [HtmlAttributeName(NameAttributeName)]
    public string? Name { get; set; }

    [HtmlAttributeName(EmailNameAttributeName)]
    public string? EmailName { get; set; }

    [HtmlAttributeName(AutofillAttributeName)]
    public bool Autofill { get; set; } = true;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (_httpContextAccessor.HttpContext is null)
        {
            return;
        }

        var tokens = _antiforgery.GetTokens(_httpContextAccessor.HttpContext);
        var buttonAttributes = output.Attributes
            .Where(x => !string.Equals(x.Name, OperationAttributeName, StringComparison.Ordinal)
                        && !string.Equals(x.Name, NameAttributeName, StringComparison.Ordinal)
                        && !string.Equals(x.Name, EmailNameAttributeName, StringComparison.Ordinal)
                        && !string.Equals(x.Name, AutofillAttributeName, StringComparison.Ordinal))
            .ToList();
        var buttonContent = (await output.GetChildContentAsync(Default)).GetContent(Default);
        await using var htmlWriter = new StringWriter();
        await htmlWriter.WriteAsync(ButtonOpeningTag);
        foreach (var buttonAttribute in buttonAttributes)
        {
            buttonAttribute.WriteTo(htmlWriter, Default);
            await htmlWriter.WriteAsync(" ");
        }

        await htmlWriter.WriteAsync(">");
        if (!IsNullOrWhiteSpace(buttonContent))
        {
            await htmlWriter.WriteAsync(buttonContent);
        }

        await htmlWriter.WriteAsync(ButtonClosingTag);
        await htmlWriter.WriteLineAsync();
        await htmlWriter.WriteAsync($"<{TagName} ");
        await WriteAttributeAsync(htmlWriter, OperationAttributeName, Operation?.ToString());
        await WriteAttributeAsync(htmlWriter, NameAttributeName, Name);
        await WriteAttributeAsync(htmlWriter, EmailNameAttributeName, EmailName);
        await WriteAttributeAsync(htmlWriter, RequestTokenNameAttributeName, tokens.HeaderName);
        await WriteAttributeAsync(htmlWriter, RequestTokenValueAttributeName, tokens.RequestToken);
        await WriteAttributeAsync(htmlWriter, AutofillAttributeName, Autofill ? AutofillOn : AutofillOff);
        await htmlWriter.WriteAsync(">");
        await htmlWriter.WriteAsync($"</{TagName}>");
        output.TagName = null;
        output.Attributes.Clear();
        output.Content.Clear();
        output.Content.SetHtmlContent(htmlWriter.ToString());
        await base.ProcessAsync(context, output);
    }

    private static async Task WriteAttributeAsync(TextWriter writer, string name, string? value)
    {
        if (!IsNullOrWhiteSpace(value))
        {
            await writer.WriteAsync($"{name}=\"{value}\" ");
        }
    }
}