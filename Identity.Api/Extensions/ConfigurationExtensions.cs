namespace Identity.Extensions;

public static class ConfigurationExtensions
{
    extension(IConfiguration configuration)
    {
        public T GetRequired<T>(string key)
            where T : notnull
        {
            return configuration.GetValue<T?>(key) ?? throw new InvalidOperationException($"Invalid '{key}'.");
        }

#pragma warning disable SA1009
        internal (
            string GoogleClientId,
            string GoogleClientSecret,
            string ResendApiToken,
            string GravatarApiSecretKey,
            string ReCAPTCHASiteKey,
            string ReCAPTCHASecretKey
        ) GetIdentitySecrets()
        {
            var googleClientId = configuration.GetRequired<string>("GoogleClientId");
            var googleClientSecret = configuration.GetRequired<string>("GoogleClientSecret");
            var resendApiToken = configuration.GetRequired<string>("ResendApiToken");
            var gravatarApiSecretKey = configuration.GetRequired<string>("GravatarApiSecretKey");
            var recaptchaSiteKey = configuration.GetRequired<string>("ReCAPTCHASiteKey");
            var recaptchaSecretKey = configuration.GetRequired<string>("ReCAPTCHASecretKey");
            return (
                googleClientId,
                googleClientSecret,
                resendApiToken,
                gravatarApiSecretKey,
                recaptchaSiteKey,
                recaptchaSecretKey
            );
        }
#pragma warning restore SA1009
    }
}
