namespace Kin.KinHub.Shared.Kernel.Exceptions;

public abstract class SharedDomainException : Exception
{
    protected SharedDomainException(string message)
        : base(message)
    {
    }
}
