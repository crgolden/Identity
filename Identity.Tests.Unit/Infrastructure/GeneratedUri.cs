namespace Identity.Tests.Unit.Infrastructure;

public static class GeneratedUri
{
    public static Uri NewLocalReturnUrl() => new(Generated.NewLocalPath(), UriKind.Relative);

    public static Uri NewExternalReturnUrl() => new(Generated.NewOrigin(), UriKind.Absolute);
}
