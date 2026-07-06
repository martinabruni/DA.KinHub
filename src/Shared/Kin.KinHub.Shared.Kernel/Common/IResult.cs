namespace Kin.KinHub.Shared.Kernel.Common;

public interface IResult
{
    bool IsSuccess { get; }
    ResultStatus Status { get; }
    string? Code { get; }
    string? Message { get; }
    object? Value { get; }
}

public interface IResult<out T> : IResult
{
    new T? Value { get; }
}
