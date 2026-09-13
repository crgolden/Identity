namespace Identity.Extensions;

public static class PasskeyEndpoints
{
    internal const string RateLimiterPolicyName = "passkey";

    internal const string AccountGroupPrefix = "/Account";

    internal const string CreationOptionsRoute = "/PasskeyCreationOptions";

    internal const string RequestOptionsRoute = "/PasskeyRequestOptions";

    internal const string CreationOptionsPath = AccountGroupPrefix + CreationOptionsRoute;

    internal const string RequestOptionsPath = AccountGroupPrefix + RequestOptionsRoute;

    internal const string UserNameQueryKey = "username";

    internal const string FallbackUserName = "User";
}
