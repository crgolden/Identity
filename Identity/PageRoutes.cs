namespace Identity;

internal static class PageRoutes
{
    internal const string ContentRoot = "~/";

    internal const string Home = "/Index";

    internal const string Login = "/Account/Login";

    internal const string Register = "/Account/Register";

    internal const string ForgotPassword = "/Account/ForgotPassword";

    internal const string ResendEmailConfirmation = "/Account/ResendEmailConfirmation";

    internal const string Logout = "/Account/Logout";

    internal const string Lockout = "/Account/Lockout";

    internal const string Consent = "/Account/Manage/Consent";

    internal const string Error = "/Error";

    internal const string Health = "/health";

    internal const string SiblingIndex = "./Index";

    internal const string SiblingDetails = "./Details";

    internal const string SiblingDetailsIndex = "./Details/Index";

    internal const string SiblingLockout = "./Lockout";

    internal const string SiblingTwoFactorAuthentication = "./TwoFactorAuthentication";

    internal const string SiblingShowRecoveryCodes = "./ShowRecoveryCodes";

    internal const string SiblingPasskeys = "./Passkeys";

    internal const string SiblingRenamePasskey = "./RenamePasskey";

    internal const string SiblingResetPasswordConfirmation = "./ResetPasswordConfirmation";

    internal const string SiblingForgotPasswordConfirmation = "./ForgotPasswordConfirmation";
}
