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
            string AdminEmail,
            string GoogleClientId,
            string GoogleClientSecret,
            string ServiceBusConnectionString,
            string GravatarApiSecretKey,
            string ReCAPTCHASiteKey,
            string ReCAPTCHASecretKey,
            string? TestEmail
        ) GetIdentitySecrets()
        {
            var adminEmail = configuration.GetValue<string>("AdminEmail");
            var googleClientId = configuration.GetRequired<string>("GoogleClientId");
            var googleClientSecret = configuration.GetRequired<string>("GoogleClientSecret");
            var serviceBusConnectionString = configuration.GetRequired<string>("ServiceBusConnectionString");
            var gravatarApiSecretKey = configuration.GetRequired<string>("GravatarApiSecretKey");
            var recaptchaSiteKey = configuration.GetRequired<string>("ReCAPTCHASiteKey");
            var recaptchaSecretKey = configuration.GetRequired<string>("ReCAPTCHASecretKey");
            var testEmail = configuration.GetValue<string?>("TestEmail");
            return (
                adminEmail,
                googleClientId,
                googleClientSecret,
                serviceBusConnectionString,
                gravatarApiSecretKey,
                recaptchaSiteKey,
                recaptchaSecretKey,
                testEmail
            );
        }
#pragma warning restore SA1009
    }
}
