namespace Kin.KinHub.Shared.Kernel.Exceptions;

using Kin.KinHub.Shared.Kernel.Common;

public sealed class EntityNotFoundException : SharedDomainException
{
    public EntityNotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.") { }
}
