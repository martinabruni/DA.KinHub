namespace Kin.KinHub.Shared.Kernel.Common;

public abstract class SharedDomainException : Exception
{
    protected SharedDomainException(string message)
        : base(message)
    {
    }
}
