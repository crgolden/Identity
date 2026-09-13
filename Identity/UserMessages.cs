namespace Identity;

using System.Globalization;

internal static class UserMessages
{
    internal const string UnableToLoadUserFormat = "Unable to load user with ID '{0}'.";

    internal const string UnableToLoadPasskeyFormat = "Unable to load passkey ID '{0}'.";

    internal const string UnableToLoadUserByEmailFormat = "Unable to load user with email '{0}'.";

    internal const string UnableToLoadTwoFactorUser = "Unable to load two-factor authentication user.";

    internal const string ConfirmEmailSubject = "Confirm your email";

    internal const string ResetPasswordSubject = "Reset Password";

    internal const string UnexpectedErrorLoadingExternalLoginInfo =
        "Unexpected error occurred loading external login info.";

    internal static string UnableToLoadUser(string? userId) =>
        Format(CultureInfo.InvariantCulture, UnableToLoadUserFormat, userId);

    internal static string UnableToLoadPasskey(string? passkeyId) =>
        Format(CultureInfo.InvariantCulture, UnableToLoadPasskeyFormat, passkeyId);

    internal static string UnableToLoadUserByEmail(string? email) =>
        Format(CultureInfo.InvariantCulture, UnableToLoadUserByEmailFormat, email);
}
