using Microsoft.Extensions.Configuration;

namespace Chatty.BE.Infrastructure.Config;

internal static class ConfigurationHelpers
{
    public static string GetString(
        IConfiguration configuration,
        string envKey,
        string configKey,
        bool required = true,
        string? defaultValue = null
    )
    {
        var envValue = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue;
        }

        var configValue = configuration[configKey];
        if (!string.IsNullOrWhiteSpace(configValue))
        {
            return configValue!;
        }

        if (defaultValue is not null)
        {
            return defaultValue;
        }

        if (required)
        {
            throw new InvalidOperationException(
                $"Missing configuration value for {envKey}/{configKey}."
            );
        }

        return string.Empty;
    }

    public static int GetInt(
        IConfiguration configuration,
        string envKey,
        string configKey,
        int defaultValue
    )
    {
        var str = GetString(configuration, envKey, configKey, required: false);
        return int.TryParse(str, out var parsed) && parsed > 0 ? parsed : defaultValue;
    }
}
