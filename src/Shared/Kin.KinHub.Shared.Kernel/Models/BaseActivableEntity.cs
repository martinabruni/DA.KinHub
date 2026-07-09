namespace Kin.KinHub.Shared.Kernel.Models;

using Kin.KinHub.Shared.Kernel.Interfaces;

public abstract class BaseActivableEntity<T> : BaseEntity<T>, IActivable
{
    public bool IsActive { get; set; }
}
