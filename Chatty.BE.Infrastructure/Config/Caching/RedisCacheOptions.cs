namespace Chatty.BE.Infrastructure.Config.Caching;

public sealed class RedisCacheOptions
{
    public const string SectionName = "RedisCache";

    public bool Enabled { get; set; } = false;

    public string Configuration { get; set; } = "localhost:6379";

    public string InstanceName { get; set; } = "chatty:be:";

    public int UserCacheSeconds { get; set; } = 300;

    public int ConversationCacheSeconds { get; set; } = 120;

    public int MessageCacheSeconds { get; set; } = 60;

    public static RedisCacheOptions Build(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);

        return new RedisCacheOptions
        {
            Enabled = bool.TryParse(section["Enabled"], out var enabled) && enabled,
            Configuration = section["Configuration"] ?? "localhost:6379",
            InstanceName = section["InstanceName"] ?? "chatty:be:",
            UserCacheSeconds = int.TryParse(section["UserCacheSeconds"], out var userTtl)
                ? userTtl
                : 300,
            ConversationCacheSeconds = int.TryParse(
                section["ConversationCacheSeconds"],
                out var conversationTtl
            )
                ? conversationTtl
                : 120,
            MessageCacheSeconds = int.TryParse(section["MessageCacheSeconds"], out var messageTtl)
                ? messageTtl
                : 60,
        };
    }
}
