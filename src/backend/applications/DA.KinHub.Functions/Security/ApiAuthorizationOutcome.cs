using Microsoft.AspNetCore.Http;

namespace DA.KinHub.Functions.Security;

public sealed class ApiAuthorizationOutcome
{
    private ApiAuthorizationOutcome(bool succeeded, AuthorizedRequest? request, int statusCode, string title, string detail, string code)
    {
        Succeeded = succeeded;
        Request = request;
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
        Code = code;
    }

    public bool Succeeded { get; }

    public AuthorizedRequest? Request { get; }

    public int StatusCode { get; }

    public string Title { get; }

    public string Detail { get; }

    public string Code { get; }

    public static ApiAuthorizationOutcome Success(AuthorizedRequest request) =>
        new(true, request, StatusCodes.Status200OK, string.Empty, string.Empty, string.Empty);

    public static ApiAuthorizationOutcome Failure(int statusCode, string title, string detail, string code) =>
        new(false, null, statusCode, title, detail, code);
}
