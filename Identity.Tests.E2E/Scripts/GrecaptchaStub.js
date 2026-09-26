window.grecaptcha = { ready: cb => cb(), execute: () => Promise.resolve(crypto.randomUUID()) };
