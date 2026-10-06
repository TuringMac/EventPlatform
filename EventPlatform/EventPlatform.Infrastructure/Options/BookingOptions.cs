using System;
using System.Collections.Generic;
using System.Text;

namespace EventPlatform.Infrastructure.Options;

public class BookingOptions
{
    public const string SectionName = "Booking";
    public int PerUserLimit { get; set; }
}
