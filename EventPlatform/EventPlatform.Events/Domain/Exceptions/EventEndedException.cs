using System;
using System.Collections.Generic;
using System.Text;

namespace EventPlatform.Events.Domain.Exceptions;

public class EventEndedException : Exception
{
    public EventEndedException(string message) : base(message) { }
}
