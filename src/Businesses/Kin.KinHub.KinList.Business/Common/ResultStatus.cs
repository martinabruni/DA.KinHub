namespace Kin.KinHub.KinList.Business.Common;

public enum ResultStatus
{
    Success,
    NotFound,
    Conflict,
    ValidationError,
    UnprocessableEntity,
    Unauthorized,
    ServiceUnavailable,
    UnexpectedError,
}
