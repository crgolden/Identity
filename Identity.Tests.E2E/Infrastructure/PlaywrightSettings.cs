namespace Identity.Tests.E2E.Infrastructure;

using System.Globalization;
using Identity.Extensions;
using Microsoft.Extensions.Configuration;

public sealed class PlaywrightSettings
{
    private PlaywrightSettings(IConfiguration section)
    {
        Headless = bool.Parse(section.GetRequired<string>(nameof(Headless)));
        TestResultsFolderName = section.GetRequired<string>(nameof(TestResultsFolderName));
        ArtifactsFolderName = section.GetRequired<string>(nameof(ArtifactsFolderName));
        TempFolderName = section.GetRequired<string>(nameof(TempFolderName));
        ScreenshotFileName = section.GetRequired<string>(nameof(ScreenshotFileName));
        TraceFileName = section.GetRequired<string>(nameof(TraceFileName));
        BrowserLogFileName = section.GetRequired<string>(nameof(BrowserLogFileName));
        MetadataFileName = section.GetRequired<string>(nameof(MetadataFileName));
        FailureFileName = section.GetRequired<string>(nameof(FailureFileName));
        RecordedVideoWidth = ParseCount(section, nameof(RecordedVideoWidth));
        RecordedVideoHeight = ParseCount(section, nameof(RecordedVideoHeight));
        MaxArtifactNameLength = ParseCount(section, nameof(MaxArtifactNameLength));
    }

    public bool Headless { get; }

    public string TestResultsFolderName { get; }

    public string ArtifactsFolderName { get; }

    public string TempFolderName { get; }

    public string ScreenshotFileName { get; }

    public string TraceFileName { get; }

    public string BrowserLogFileName { get; }

    public string MetadataFileName { get; }

    public string FailureFileName { get; }

    public int RecordedVideoWidth { get; }

    public int RecordedVideoHeight { get; }

    public int MaxArtifactNameLength { get; }

    public static PlaywrightSettings Read(IConfiguration configuration) =>
        new(configuration.GetRequiredSection(nameof(PlaywrightSettings)));

    private static int ParseCount(IConfiguration section, string key) =>
        int.Parse(section.GetRequired<string>(key), CultureInfo.InvariantCulture);
}
