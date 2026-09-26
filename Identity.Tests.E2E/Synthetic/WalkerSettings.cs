namespace Identity.Tests.E2E.Synthetic;

using System.Globalization;
using Identity.Extensions;
using Microsoft.Extensions.Configuration;

public sealed class WalkerSettings
{
    private WalkerSettings(IConfiguration section)
    {
        BrowserUserAgent = section.GetRequired<string>(nameof(BrowserUserAgent));
        SyntheticUserAgentVersion = section.GetRequired<string>(nameof(SyntheticUserAgentVersion));
        CommonActionWeight = ParseCount(section, nameof(CommonActionWeight));
        RareActionWeight = ParseCount(section, nameof(RareActionWeight));
        SectionActionWeight = ParseCount(section, nameof(SectionActionWeight));
        MemberSlot = ParseCount(section, nameof(MemberSlot));
        AdminSlot = ParseCount(section, nameof(AdminSlot));
        DefaultStepBudget = ParseCount(section, nameof(DefaultStepBudget));
        MaxStepBudget = ParseCount(section, nameof(MaxStepBudget));
    }

    public string BrowserUserAgent { get; }

    public string SyntheticUserAgentVersion { get; }

    public int CommonActionWeight { get; }

    public int RareActionWeight { get; }

    public int SectionActionWeight { get; }

    public int MemberSlot { get; }

    public int AdminSlot { get; }

    public int DefaultStepBudget { get; }

    public int MaxStepBudget { get; }

    public static WalkerSettings Read(IConfiguration configuration) =>
        new(configuration.GetRequiredSection(nameof(WalkerSettings)));

    private static int ParseCount(IConfiguration section, string key) =>
        int.Parse(section.GetRequired<string>(key), CultureInfo.InvariantCulture);
}
