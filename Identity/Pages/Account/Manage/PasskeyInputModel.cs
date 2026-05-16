namespace Identity.Pages.Account.Manage;

/// <summary>Holds the passkey credential JSON and optional error message submitted from the client-side WebAuthn handler.</summary>
public class PasskeyInputModel
{
    public string? CredentialJson { get; set; }

    public string? Error { get; set; }
}
