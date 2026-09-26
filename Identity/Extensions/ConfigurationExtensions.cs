namespace Identity.Extensions;

public static class ConfigurationExtensions
{
    extension(IConfiguration configuration)
    {
        public T GetRequired<T>(string key)
            where T : notnull
        {
            if (configuration[key] is null)
            {
                throw new InvalidOperationException($"Invalid '{key}'.");
            }

            return configuration.GetValue<T?>(key) ?? throw new InvalidOperationException($"Invalid '{key}'.");
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
