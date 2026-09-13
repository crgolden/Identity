namespace Identity.Tests.Unit.PropertyBased;

internal static class SanitizationFixtureConstants
{
    internal const int MinGeneratedInputLength = 1;
    internal const int MaxGeneratedInputLength = 200;
    internal const int MaxNormalizedInputLength = 50;
    internal const char PathSeparator = '/';
    internal const char TabCharacter = '\t';
    internal const char SpaceCharacter = ' ';
    internal const char QueryStringStart = '?';
    internal const char QueryStringAssignment = '=';
    internal const char SchemeSeparator = ':';
    internal const char DataUrlSeparator = ',';
    internal const string JavaScriptScheme = "javascript";
    internal const string DataScheme = "data";
}
