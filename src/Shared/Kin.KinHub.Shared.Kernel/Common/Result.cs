namespace Kin.KinHub.Shared.Kernel.Common;

public sealed class Result<T>
{
    public bool IsSuccess => Status is ResultStatus.Success;
    public ResultStatus Status { get; private init; }
    public string? Code { get; private init; }
    public string? Message { get; private init; }
    public T? Value { get; private init; }

    private Result() { }

    public static Result<T> Success(T value) =>
        new() { Status = ResultStatus.Success, Value = value };

    public static Result<T> NotFound(string message, string code = "not_found") =>
        new() { Status = ResultStatus.NotFound, Message = message, Code = code };

    public static Result<T> Conflict(string message, string code = "conflict") =>
        new() { Status = ResultStatus.Conflict, Message = message, Code = code };

    public static Result<T> ValidationError(string message, string code = "validation_error") =>
        new() { Status = ResultStatus.ValidationError, Message = message, Code = code };

    public static Result<T> UnprocessableEntity(string message, string code = "unprocessable_entity") =>
        new() { Status = ResultStatus.UnprocessableEntity, Message = message, Code = code };

    public static Result<T> Unauthorized(string message, string code = "forbidden") =>
        new() { Status = ResultStatus.Unauthorized, Message = message, Code = code };

    public static Result<T> Forbidden(string message, string code = "forbidden") =>
        new() { Status = ResultStatus.Unauthorized, Message = message, Code = code };

    public static Result<T> Unauthenticated(string message, string code = "authentication_required") =>
        new() { Status = ResultStatus.Unauthorized, Message = message, Code = code };

    public static Result<T> ServiceUnavailable(string message, string code = "service_unavailable") =>
        new() { Status = ResultStatus.ServiceUnavailable, Message = message, Code = code };

    public static Result<T> UnexpectedError(string message, string code = "unexpected_error") =>
        new() { Status = ResultStatus.UnexpectedError, Message = message, Code = code };
}
