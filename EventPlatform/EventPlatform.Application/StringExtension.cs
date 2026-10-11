using EventPlatform.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace EventPlatform.Application;

public static class StringExtension
{
    public static string ToHashString(this string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        var hashedPassword = Convert.ToHexString(bytes);
        return hashedPassword;
    }
}
