namespace Identity.Tests.E2E.Infrastructure;

using System.Text.RegularExpressions;

internal static partial class UrlPatterns
{
    private const string DeleteAnywherePattern = "Delete";

    private const string DeletePagePattern = "/Delete";

    private const string RegisterConfirmationPattern = "/Account/RegisterConfirmation";

    private const string LoginWith2faPattern = "/Account/LoginWith2fa";

    private const string LoginWithRecoveryCodePattern = "/Account/LoginWithRecoveryCode";

    private const string ForgotPasswordConfirmationPattern = "/Account/ForgotPasswordConfirmation";

    private const string ResetPasswordConfirmationPattern = "/Account/ResetPasswordConfirmation";

    private const string ManagePattern = "/Account/Manage";

    private const string ManageEmailPattern = "/Account/Manage/Email";

    private const string ManageTwoFactorAuthenticationPattern = "/Account/Manage/TwoFactorAuthentication";

    private const string ManageResetAuthenticatorPattern = "/Account/Manage/ResetAuthenticator";

    private const string ShowRecoveryCodesOrTwoFactorAuthenticationPattern = "ShowRecoveryCodes|TwoFactorAuthentication";

    private const string AdminClientsDetailsPattern = "/Admin/Clients/Details";

    private const string AdminApiResourcesDetailsPattern = "/Admin/ApiResources/Details";

    private const string AdminApiScopesDetailsPattern = "/Admin/ApiScopes/Details";

    private const string AdminIdentityResourcesDetailsPattern = "/Admin/IdentityResources/Details";

    private const string AdminIdentityProvidersDetailsPattern = "/Admin/IdentityProviders/Details";

    private const string AdminIdentityProvidersEditPattern = "/Admin/IdentityProviders/Edit";

    private const string AdminSamlServiceProvidersDetailsPattern = "/Admin/SamlServiceProviders/Details";

    private const string AdminSamlServiceProvidersEditPattern = "/Admin/SamlServiceProviders/Edit";

    private const string AdminPersistedGrantsDetailsPattern = "/Admin/PersistedGrants/Details";

    private const string AdminUsersDetailsIndexPattern = "/Admin/Users/Details/(?!Claims|Roles|Logins|Passkeys)";

    private const string AdminUsersEditIndexPattern = "/Admin/Users/Edit/(?!Claims|Roles|Logins|Passkeys)";

    private const string AdminUsersEditClaimsPattern = "/Admin/Users/Edit/Claims";

    private const string AdminUsersEditRolesPattern = "/Admin/Users/Edit/Roles";

    private const string AdminRolesDetailsPattern = "/Admin/Roles/Details";

    private const string AdminRolesDetailsIndexPattern = "/Admin/Roles/Details/(?!Claims|Users)";

    private const string AdminRolesEditIndexPattern = "/Admin/Roles/Edit/(?!Claims|Users)";

    private const string AdminRolesEditClaimsPattern = "/Admin/Roles/Edit/Claims";

    [GeneratedRegex(PageRoutes.Login)]
    internal static partial Regex Login();

    [GeneratedRegex(PageRoutes.Register)]
    internal static partial Regex Register();

    [GeneratedRegex(DeleteAnywherePattern)]
    internal static partial Regex DeleteAnywhere();

    [GeneratedRegex(DeletePagePattern)]
    internal static partial Regex DeletePage();

    [GeneratedRegex(RegisterConfirmationPattern)]
    internal static partial Regex RegisterConfirmation();

    [GeneratedRegex(LoginWith2faPattern)]
    internal static partial Regex LoginWith2fa();

    [GeneratedRegex(LoginWithRecoveryCodePattern)]
    internal static partial Regex LoginWithRecoveryCode();

    [GeneratedRegex(ForgotPasswordConfirmationPattern)]
    internal static partial Regex ForgotPasswordConfirmation();

    [GeneratedRegex(ResetPasswordConfirmationPattern)]
    internal static partial Regex ResetPasswordConfirmation();

    [GeneratedRegex(ManagePattern)]
    internal static partial Regex Manage();

    [GeneratedRegex(ManageEmailPattern)]
    internal static partial Regex ManageEmail();

    [GeneratedRegex(ManageTwoFactorAuthenticationPattern)]
    internal static partial Regex ManageTwoFactorAuthentication();

    [GeneratedRegex(ManageResetAuthenticatorPattern)]
    internal static partial Regex ManageResetAuthenticator();

    [GeneratedRegex(ShowRecoveryCodesOrTwoFactorAuthenticationPattern)]
    internal static partial Regex ShowRecoveryCodesOrTwoFactorAuthentication();

    [GeneratedRegex(Pages.Account.Manage.ExternalLogins.ExternalLoginsPagePath)]
    internal static partial Regex ManageExternalLogins();

    [GeneratedRegex(AdminClientsDetailsPattern)]
    internal static partial Regex AdminClientsDetails();

    [GeneratedRegex(Pages.Admin.Clients.Edit.Claims.DetailsPageName)]
    internal static partial Regex AdminClientsDetailsClaims();

    [GeneratedRegex(Pages.Admin.Clients.Edit.CorsOrigins.DetailsPageName)]
    internal static partial Regex AdminClientsDetailsCorsOrigins();

    [GeneratedRegex(Pages.Admin.Clients.Edit.GrantTypes.DetailsPageName)]
    internal static partial Regex AdminClientsDetailsGrantTypes();

    [GeneratedRegex(Pages.Admin.Clients.Edit.IdPRestrictions.DetailsPageName)]
    internal static partial Regex AdminClientsDetailsIdPRestrictions();

    [GeneratedRegex(Pages.Admin.Clients.Edit.PostLogoutRedirectUris.DetailsPageName)]
    internal static partial Regex AdminClientsDetailsPostLogoutRedirectUris();

    [GeneratedRegex(Pages.Admin.Clients.Edit.Properties.DetailsPageName)]
    internal static partial Regex AdminClientsDetailsProperties();

    [GeneratedRegex(Pages.Admin.Clients.Edit.RedirectUris.DetailsPageName)]
    internal static partial Regex AdminClientsDetailsRedirectUris();

    [GeneratedRegex(Pages.Admin.Clients.Edit.Scopes.DetailsPageName)]
    internal static partial Regex AdminClientsDetailsScopes();

    [GeneratedRegex(Pages.Admin.Clients.Edit.Secrets.DetailsPageName)]
    internal static partial Regex AdminClientsDetailsSecrets();

    [GeneratedRegex(AdminApiResourcesDetailsPattern)]
    internal static partial Regex AdminApiResourcesDetails();

    [GeneratedRegex(Pages.Admin.ApiResources.Edit.ClaimTypes.DetailsPageName)]
    internal static partial Regex AdminApiResourcesDetailsClaimTypes();

    [GeneratedRegex(Pages.Admin.ApiResources.Edit.Properties.DetailsPageName)]
    internal static partial Regex AdminApiResourcesDetailsProperties();

    [GeneratedRegex(Pages.Admin.ApiResources.Edit.Scopes.DetailsPageName)]
    internal static partial Regex AdminApiResourcesDetailsScopes();

    [GeneratedRegex(Pages.Admin.ApiResources.Edit.Secrets.DetailsPageName)]
    internal static partial Regex AdminApiResourcesDetailsSecrets();

    [GeneratedRegex(AdminApiScopesDetailsPattern)]
    internal static partial Regex AdminApiScopesDetails();

    [GeneratedRegex(Pages.Admin.ApiScopes.Edit.ClaimTypes.DetailsPageName)]
    internal static partial Regex AdminApiScopesDetailsClaimTypes();

    [GeneratedRegex(Pages.Admin.ApiScopes.Edit.Properties.DetailsPageName)]
    internal static partial Regex AdminApiScopesDetailsProperties();

    [GeneratedRegex(AdminIdentityResourcesDetailsPattern)]
    internal static partial Regex AdminIdentityResourcesDetails();

    [GeneratedRegex(Pages.Admin.IdentityResources.Edit.ClaimTypes.DetailsPageName)]
    internal static partial Regex AdminIdentityResourcesDetailsClaimTypes();

    [GeneratedRegex(Pages.Admin.IdentityResources.Edit.Properties.DetailsPageName)]
    internal static partial Regex AdminIdentityResourcesDetailsProperties();

    [GeneratedRegex(AdminIdentityProvidersDetailsPattern)]
    internal static partial Regex AdminIdentityProvidersDetails();

    [GeneratedRegex(AdminIdentityProvidersEditPattern)]
    internal static partial Regex AdminIdentityProvidersEdit();

    [GeneratedRegex(AdminSamlServiceProvidersDetailsPattern)]
    internal static partial Regex AdminSamlServiceProvidersDetails();

    [GeneratedRegex(AdminSamlServiceProvidersEditPattern)]
    internal static partial Regex AdminSamlServiceProvidersEdit();

    [GeneratedRegex(AdminPersistedGrantsDetailsPattern)]
    internal static partial Regex AdminPersistedGrantsDetails();

    [GeneratedRegex(AdminUsersDetailsIndexPattern)]
    internal static partial Regex AdminUsersDetailsIndex();

    [GeneratedRegex(Pages.Admin.Users.Edit.Claims.DetailsPageName)]
    internal static partial Regex AdminUsersDetailsClaims();

    [GeneratedRegex(Pages.Admin.Users.Details.Logins.PageName)]
    internal static partial Regex AdminUsersDetailsLogins();

    [GeneratedRegex(Pages.Admin.Users.Details.Passkeys.PageName)]
    internal static partial Regex AdminUsersDetailsPasskeys();

    [GeneratedRegex(Pages.Admin.Users.Edit.Roles.DetailsPageName)]
    internal static partial Regex AdminUsersDetailsRoles();

    [GeneratedRegex(AdminUsersEditIndexPattern)]
    internal static partial Regex AdminUsersEditIndex();

    [GeneratedRegex(AdminUsersEditClaimsPattern)]
    internal static partial Regex AdminUsersEditClaims();

    [GeneratedRegex(AdminUsersEditRolesPattern)]
    internal static partial Regex AdminUsersEditRoles();

    [GeneratedRegex(AdminRolesDetailsPattern)]
    internal static partial Regex AdminRolesDetails();

    [GeneratedRegex(AdminRolesDetailsIndexPattern)]
    internal static partial Regex AdminRolesDetailsIndex();

    [GeneratedRegex(Pages.Admin.Roles.Edit.Claims.DetailsPageName)]
    internal static partial Regex AdminRolesDetailsClaims();

    [GeneratedRegex(Pages.Admin.Roles.Details.Users.PageName)]
    internal static partial Regex AdminRolesDetailsUsers();

    [GeneratedRegex(AdminRolesEditIndexPattern)]
    internal static partial Regex AdminRolesEditIndex();

    [GeneratedRegex(AdminRolesEditClaimsPattern)]
    internal static partial Regex AdminRolesEditClaims();
}
