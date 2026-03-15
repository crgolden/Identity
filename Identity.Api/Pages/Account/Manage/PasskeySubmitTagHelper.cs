namespace Identity.Pages.Account.Manage;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Razor.TagHelpers;
using static Microsoft.AspNetCore.Razor.TagHelpers.NullHtmlEncoder;

[HtmlTargetElement("passkey-submit")]
public class PasskeySubmitTagHelper : TagHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAntiforgery _antiforgery;

    public PasskeySubmitTagHelper(IHttpContextAccessor httpContextAccessor, IAntiforgery antiforgery)
    {
        _httpContextAccessor = httpContextAccessor;
        _antiforgery = antiforgery;
    }

    [HtmlAttributeName("operation")]
    public PasskeyOperation? Operation { get; set; }

    [HtmlAttributeName("name")]
    public string? Name { get; set; }

    [HtmlAttributeName("email-name")]
    public string? EmailName { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (_httpContextAccessor.HttpContext is null)
        {
            return;
        }

        var tokens = _antiforgery.GetTokens(_httpContextAccessor.HttpContext);
        var buttonAttributes = output.Attributes
            .Where(x => !string.Equals(x.Name, "operation") && !string.Equals(x.Name, "name") && !string.Equals(x.Name, "email-name"))
            .ToList();
        var buttonContent = (await output.GetChildContentAsync(Default)).GetContent(Default);
        const string value = "<button type=\"submit\" name=\"__passkeySubmit\" ";
        await using var htmlWriter = new StringWriter();
        await htmlWriter.WriteAsync(value);
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

        await htmlWriter.WriteAsync("</button>");
        await htmlWriter.WriteLineAsync();
        await htmlWriter.WriteAsync("<passkey-submit ");
        await htmlWriter.WriteAsync($"operation=\"{Operation}\" ");
        await htmlWriter.WriteAsync($"name=\"{Name}\" ");
        await htmlWriter.WriteAsync($"email-name=\"{EmailName ?? Empty}\" ");
        await htmlWriter.WriteAsync($"request-token-name=\"{tokens.HeaderName ?? Empty}\" ");
        await htmlWriter.WriteAsync($"request-token-value=\"{tokens.RequestToken ?? Empty}\" ");
        await htmlWriter.WriteAsync(">");
        await htmlWriter.WriteAsync("</passkey-submit>");
        output.TagName = null;
        output.Attributes.Clear();
        output.Content.Clear();
        output.Content.SetHtmlContent(htmlWriter.ToString());
        await base.ProcessAsync(context, output);
    }
}
