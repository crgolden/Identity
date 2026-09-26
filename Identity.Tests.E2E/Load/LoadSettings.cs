namespace Identity.Tests.E2E.Load;

using System.Globalization;
using Identity.Extensions;
using Microsoft.Extensions.Configuration;

public sealed class LoadSettings
{
    private LoadSettings(IConfiguration section)
    {
        RequestScale = ParseCount(section, nameof(RequestScale));
        MaxRequests = ParseCount(section, nameof(MaxRequests));
        MaxFailRate = ParseRate(section, nameof(MaxFailRate));
        MaxLoginFailRate = ParseRate(section, nameof(MaxLoginFailRate));
        DiscoveryRequests = ParseCount(section, nameof(DiscoveryRequests));
        DiscoveryParallelism = ParseCount(section, nameof(DiscoveryParallelism));
        LoginRequests = ParseCount(section, nameof(LoginRequests));
        LoginParallelism = ParseCount(section, nameof(LoginParallelism));
        JwksRequests = ParseCount(section, nameof(JwksRequests));
        JwksParallelism = ParseCount(section, nameof(JwksParallelism));
        HealthRequests = ParseCount(section, nameof(HealthRequests));
        HealthParallelism = ParseCount(section, nameof(HealthParallelism));
    }

    public int RequestScale { get; }

    public int MaxRequests { get; }

    public double MaxFailRate { get; }

    public double MaxLoginFailRate { get; }

    public int DiscoveryRequests { get; }

    public int DiscoveryParallelism { get; }

    public int LoginRequests { get; }

    public int LoginParallelism { get; }

    public int JwksRequests { get; }

    public int JwksParallelism { get; }

    public int HealthRequests { get; }

    public int HealthParallelism { get; }

    public static LoadSettings Read(IConfiguration configuration) =>
        new(configuration.GetRequiredSection(nameof(LoadSettings)));

    private static int ParseCount(IConfiguration section, string key) =>
        int.Parse(section.GetRequired<string>(key), CultureInfo.InvariantCulture);

    private static double ParseRate(IConfiguration section, string key) =>
        double.Parse(section.GetRequired<string>(key), CultureInfo.InvariantCulture);
}
