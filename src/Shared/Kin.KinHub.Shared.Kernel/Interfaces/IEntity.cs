namespace Kin.KinHub.Shared.Kernel.Interfaces;

/// <summary>
/// Represents a domain entity with a typed identifier.
/// </summary>
/// <typeparam name="T">The type of the entity identifier.</typeparam>
public interface IEntity<T>
{
    T Id { get; }
}
