namespace Kin.KinHub.Shared.Kernel.Interfaces;

/// <summary>
/// Represents an auditable entity with creation and update timestamps.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
