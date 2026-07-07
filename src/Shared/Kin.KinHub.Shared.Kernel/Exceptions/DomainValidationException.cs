namespace Kin.KinHub.Shared.Kernel.Exceptions;

using Kin.KinHub.Shared.Kernel.Common;

public sealed class DomainValidationException : SharedDomainException
{
    public DomainValidationException(string message) : base(message) { }
}
