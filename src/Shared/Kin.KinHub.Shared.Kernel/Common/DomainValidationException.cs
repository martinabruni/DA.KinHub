namespace Kin.KinHub.Shared.Kernel.Common;

public abstract class SharedDomainValidationException : SharedDomainException
{
    protected SharedDomainValidationException(string message)
        : base(message)
    {
    }
}
