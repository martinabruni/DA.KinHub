namespace Kin.KinHub.Shared.Kernel.Interfaces;

/// <summary>
/// Represents an entity that can be activated or deactivated.
/// </summary>
public interface IActivable
{
    bool IsActive { get; set; }
}
