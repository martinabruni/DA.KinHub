namespace Kin.KinHub.Core.Domain.Common;

public abstract class DomainException : SharedDomainException
{
    protected DomainException(string message) : base(message) { }
}
