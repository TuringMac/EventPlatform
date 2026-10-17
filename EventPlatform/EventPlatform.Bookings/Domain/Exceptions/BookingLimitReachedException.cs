using System;
using System.Collections.Generic;
using System.Text;

namespace EventPlatform.Bookings.Domain.Exceptions;

public class BookingLimitReachedException : Exception
{
    public BookingLimitReachedException(string message) : base(message) { }
}
