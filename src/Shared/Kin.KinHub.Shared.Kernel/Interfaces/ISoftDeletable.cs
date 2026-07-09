namespace Kin.KinHub.Shared.Kernel.Interfaces;

/// <summary>
/// Represents an entity that supports soft deletion.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}
