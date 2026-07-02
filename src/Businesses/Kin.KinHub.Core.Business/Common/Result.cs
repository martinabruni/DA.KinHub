namespace Kin.KinHub.Core.Business.Common;

public sealed class Result<T>
{
    public bool IsSuccess => Status is ResultStatus.Success;
    public ResultStatus Status { get; private init; }
    public T? Value { get; private init; }
    public string? Message { get; private init; }

    private Result() { }

    public static Result<T> Success(T value) =>
        new() { Status = ResultStatus.Success, Value = value };

    public static Result<T> NotFound(string message) =>
        new() { Status = ResultStatus.NotFound, Message = message };

    public static Result<T> Conflict(string message) =>
        new() { Status = ResultStatus.Conflict, Message = message };

    public static Result<T> ValidationError(string message) =>
        new() { Status = ResultStatus.ValidationError, Message = message };

    public static Result<T> Unauthorized(string message) =>
        new() { Status = ResultStatus.Unauthorized, Message = message };

    public static Result<T> ServiceUnavailable(string message) =>
        new() { Status = ResultStatus.ServiceUnavailable, Message = message };

    public static Result<T> UnexpectedError(string message) =>
        new() { Status = ResultStatus.UnexpectedError, Message = message };
}
