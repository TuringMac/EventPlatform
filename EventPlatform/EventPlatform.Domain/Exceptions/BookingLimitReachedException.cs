using System;
using System.Collections.Generic;
using System.Text;

namespace EventPlatform.Domain.Exceptions;

public class BookingLimitReachedException : Exception
{
    public BookingLimitReachedException(string message) : base(message) { }
}
