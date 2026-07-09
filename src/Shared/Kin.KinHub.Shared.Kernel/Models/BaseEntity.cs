namespace Kin.KinHub.Shared.Kernel.Models;

using Kin.KinHub.Shared.Kernel.Interfaces;

public abstract class BaseEntity<T> : IEntity<T>, IAuditable
{
    public required T Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
