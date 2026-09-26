namespace Identity.Tests.Unit.Infrastructure;

using static Shared.Testing.Generated;

internal static class TestValues
{
    internal static global::Identity.Pages.Account.AccountEmailSettings NewAccountEmailSettings() =>
        new(NewEmailAddress(), NewSingleArgumentFormat(), NewSingleArgumentFormat());

    internal static TelemetryOptions NewTelemetryOptions() =>
        new(NewDescription(), NewDescription(), NewDescription(), NewDescription(), NewDescription());
}
