namespace Kin.KinHub.Shared.Kernel.Exceptions;

using Kin.KinHub.Shared.Kernel.Common;

public sealed class DuplicateEntityException : SharedDomainException
{
    public DuplicateEntityException(string entityName, string field, object value)
        : base($"{entityName} with {field} '{value}' already exists.") { }
}
