namespace EventPlatform.Events.Infrastructure.Options;

public class CacheOptions
{
    public const string SectionName = "Cache";
    public int Top10TTL { get; set; }
    public int RegularTTL { get; set; }
}
