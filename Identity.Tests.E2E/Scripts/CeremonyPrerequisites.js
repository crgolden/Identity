() => {
    const element = document.querySelector('passkey-submit');
    const button = document.querySelector('button[name="__passkeySubmit"]');
    const prerequisites = {
        isSecureContext: window.isSecureContext,
        hasCredentials: navigator.credentials !== undefined,
        hasPublicKeyCredential: typeof PublicKeyCredential !== 'undefined',
        hasParseCreationOptions: typeof PublicKeyCredential !== 'undefined'
            && typeof PublicKeyCredential.parseCreationOptionsFromJSON === 'function',
        hasParseRequestOptions: typeof PublicKeyCredential !== 'undefined'
            && typeof PublicKeyCredential.parseRequestOptionsFromJSON === 'function',
        customElementDefined: customElements.get('passkey-submit') !== undefined,
        elementUpgraded: element instanceof (customElements.get('passkey-submit') ?? HTMLElement)
            && element?.internals !== undefined,
        elementSeesForm: element?.internals?.form !== null
            && element?.internals?.form !== undefined,
        operation: element?.getAttribute('operation'),
        clickTargetIsSubmitter: button?.id === 'add-passkey',
    };
    return Object.entries(prerequisites).filter(([, met]) => !met).map(([name]) => name);
}
