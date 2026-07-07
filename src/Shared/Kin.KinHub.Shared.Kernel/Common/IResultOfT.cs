namespace Kin.KinHub.Shared.Kernel.Common;

public interface IResult<out T> : IResult
{
    new T? Value { get; }
}
