namespace GymManager.API.Services;

public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
