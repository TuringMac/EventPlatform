using System;
using System.Collections.Generic;
using System.Text;

namespace EventPlatform.Infrastructure.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int Lifetime { get; set; }
}
