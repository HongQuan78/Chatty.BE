using DotNetEnv;

namespace Chatty.BE.API.Config;

public static class EnvironmentLoader
{
    public static void Load(WebApplicationBuilder builder)
    {
        var existingVars = Environment
            .GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(e => e.Key?.ToString() ?? string.Empty, e => e.Value?.ToString());

        var envFilePath = Path.Combine(builder.Environment.ContentRootPath, ".env");
        if (File.Exists(envFilePath))
        {
            Env.Load(envFilePath);
        }

        foreach (var kvp in existingVars)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }

        var defaultConnection = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");
        if (!string.IsNullOrWhiteSpace(defaultConnection))
        {
            builder.Configuration["ConnectionStrings:DefaultConnection"] = defaultConnection;
        }
    }
}
