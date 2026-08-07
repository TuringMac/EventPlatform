namespace EventPlatform.Api.Exceptions;

public class NoAvailableSeatsException : Exception
{
    public NoAvailableSeatsException(string message) : base(message) { }
}
