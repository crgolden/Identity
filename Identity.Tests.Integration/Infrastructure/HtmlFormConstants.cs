namespace Identity.Tests.Integration.Infrastructure;

internal static class HtmlFormConstants
{
    internal const string ValueGroupName = "value";

    internal const string HiddenInputValuePattern = "name=\"{0}\" type=\"hidden\" value=\"(?<" + ValueGroupName + ">[^\"]+)\"";

    internal const string AttributeValuePattern = "{0}=\"(?<" + ValueGroupName + ">[^\"]+)\"";
}
