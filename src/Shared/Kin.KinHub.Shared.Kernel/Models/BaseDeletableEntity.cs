namespace Kin.KinHub.Shared.Kernel.Models;

using Kin.KinHub.Shared.Kernel.Interfaces;

public abstract class BaseDeletableEntity<T> : BaseEntity<T>, ISoftDeletable
{
    public bool IsDeleted { get; set; }
}
