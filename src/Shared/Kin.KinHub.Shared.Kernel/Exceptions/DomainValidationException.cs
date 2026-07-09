namespace Kin.KinHub.Shared.Kernel.Exceptions;

public sealed class DomainValidationException : SharedDomainException
{
    public DomainValidationException(string message) : base(message) { }
}
