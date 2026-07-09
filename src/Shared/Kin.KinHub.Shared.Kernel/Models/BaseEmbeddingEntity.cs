namespace Kin.KinHub.Shared.Kernel.Models;

public abstract class BaseEmbeddingEntity<T> : BaseDeletableEntity<T>
{
    public float[]? Embedding { get; set; }
}
