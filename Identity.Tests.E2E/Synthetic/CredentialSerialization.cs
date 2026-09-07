namespace Identity.Tests.E2E.Synthetic;

using Microsoft.Playwright;

internal static class CredentialSerialization
{
    private const string Shim = """
(() => {
  const toBase64Url = buffer =>
    btoa(String.fromCharCode(...new Uint8Array(buffer)))
      .replaceAll('+', '-')
      .replaceAll('/', '_')
      .replace(/=+$/, '');

  const buildJson = credential => {
    const source = credential.response;
    const response = source instanceof AuthenticatorAttestationResponse
      ? {
          clientDataJSON: toBase64Url(source.clientDataJSON),
          attestationObject: toBase64Url(source.attestationObject),
          transports: source.getTransports ? source.getTransports() : [],
        }
      : {
          clientDataJSON: toBase64Url(source.clientDataJSON),
          authenticatorData: toBase64Url(source.authenticatorData),
          signature: toBase64Url(source.signature),
          userHandle: source.userHandle ? toBase64Url(source.userHandle) : undefined,
        };
    return {
      id: credential.id,
      rawId: toBase64Url(credential.rawId),
      type: credential.type,
      authenticatorAttachment: credential.authenticatorAttachment ?? undefined,
      clientExtensionResults: credential.getClientExtensionResults(),
      response,
    };
  };

  const withWorkingSerialization = credential => {
    if (credential) {
      Object.defineProperty(credential, 'toJSON', {
        configurable: true,
        writable: true,
        value: () => buildJson(credential),
      });
    }
    return credential;
  };

  const originalCreate = navigator.credentials.create.bind(navigator.credentials);
  const originalGet = navigator.credentials.get.bind(navigator.credentials);
  navigator.credentials.create = options => originalCreate(options).then(withWorkingSerialization);
  navigator.credentials.get = options => originalGet(options).then(withWorkingSerialization);
})();
""";

    public static Task InstallAsync(IBrowserContext context) => context.AddInitScriptAsync(Shim);
}
