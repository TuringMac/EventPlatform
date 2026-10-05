using System;
using System.Collections.Generic;
using System.Text;

namespace EventPlatform.Infrastructure.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int Lifetime { get; set; } = 15;
}
