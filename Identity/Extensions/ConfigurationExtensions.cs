namespace Identity.Extensions;

public static class ConfigurationExtensions
{
    extension(IConfiguration configuration)
    {
        public T GetRequired<T>(string key)
            where T : notnull
        {
            if (!IsNullOrWhiteSpace(configuration[key]) && configuration.GetValue<T?>(key) is { } value)
            {
                return value;
            }

            throw new InvalidOperationException($"Invalid '{key}'.");
        }

        public string GetRequiredText(string key)
        {
            var value = configuration[key];
            if (IsNullOrWhiteSpace(value))
            {
                var path = configuration is IConfigurationSection section ? ConfigurationPath.Combine(section.Path, key) : key;
                throw new InvalidOperationException($"Invalid '{path}'.");
            }

            return value;
        }
    }
}
