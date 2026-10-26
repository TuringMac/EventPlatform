using System;
using System.Collections.Generic;
using System.Text;

namespace EventPlatform.Bookings.Domain.Exceptions;

public class BookingAlreadyCancelledException : Exception
{
    public BookingAlreadyCancelledException(string message) : base(message) { }
}
