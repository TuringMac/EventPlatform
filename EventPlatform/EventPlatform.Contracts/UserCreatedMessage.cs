using System;
using System.Collections.Generic;
using System.Text;

namespace EventPlatform.Contracts;

public class UserCreatedMessage
{
    public Guid UserId { get; set; }
}
