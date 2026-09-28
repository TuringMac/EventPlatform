using System;
using System.Collections.Generic;
using System.Text;

namespace EventPlatform.Domain.Exceptions;

public class EventEndedException : Exception
{
    public EventEndedException(string message) : base(message) { }
}
