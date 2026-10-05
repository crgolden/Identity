namespace Identity.Tests.E2E.Infrastructure;

using System.Globalization;
using Identity.Extensions;
using Microsoft.Extensions.Configuration;

public sealed class E2ESettings
{
    private E2ESettings(IConfiguration section)
    {
        IndependentlyPinnedAdminCardCount = int.Parse(
            section.GetRequired<string>(nameof(IndependentlyPinnedAdminCardCount)), CultureInfo.InvariantCulture);
    }

    public int IndependentlyPinnedAdminCardCount { get; }

    public static E2ESettings Read(IConfiguration configuration) =>
        new(configuration.GetRequiredSection(nameof(E2ESettings)));
}
