using System;
using System.Collections.Generic;
using System.Text;

namespace EventPlatform.Domain.Exceptions;

public class UserAlreadyExistsException : Exception
{
    public UserAlreadyExistsException(string message) : base(message) { }
}
