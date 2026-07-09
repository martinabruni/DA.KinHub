namespace Kin.KinHub.Shared.Kernel.Exceptions;

public sealed class DuplicateEntityException : SharedDomainException
{
    public DuplicateEntityException(string entityName, string field, object value)
        : base($"{entityName} with {field} '{value}' already exists.") { }
}
