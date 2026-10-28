using System;
using System.Collections.Generic;
using System.Text;

namespace EventPlatform.Events.Infrastructure.Options;

public class RedisOptions
{
    public const string SectionName = "Redis";

    public string Host { get; set; } = null!;
    public int Port { get; set; }
}
