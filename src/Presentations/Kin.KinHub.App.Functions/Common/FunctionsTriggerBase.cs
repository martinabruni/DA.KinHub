using System.Text.Json;

namespace Kin.KinHub.App.Functions.Common;

public abstract class FunctionsTriggerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected static ControllerBase CreateController(HttpRequest request) => new FunctionController(request.HttpContext);

    protected static IActionResult ToKinListActionResult<T>(HttpRequest request, Result<T> result) =>
        SharedHttpResultMapper.ToActionResult(CreateController(request), result, unauthorizedIsForbidden: true);

    protected static IActionResult ToKinListCreatedActionResult<T>(HttpRequest request, Result<T> result) =>
        SharedHttpResultMapper.ToCreatedActionResult(CreateController(request), result, unauthorizedIsForbidden: true);

    protected static IActionResult ToActionResult<T>(HttpRequest request, Result<T> result) =>
        HttpResultMapper.ToActionResult(CreateController(request), result);

    protected static IActionResult ToCreatedActionResult<T>(HttpRequest request, Result<T> result) =>
        HttpResultMapper.ToCreatedActionResult(CreateController(request), result);

    protected static string? ReadIfMatch(HttpRequest request)
    {
        var ifMatch = request.Headers.IfMatch.ToString();
        return string.IsNullOrWhiteSpace(ifMatch) ? null : ifMatch.Trim();
    }

    protected static async Task<(TRequest? Request, IActionResult? Error)> ReadAndValidateAsync<TRequest>(
        HttpRequest request,
        IRequestValidator<TRequest> validator,
        CancellationToken cancellationToken)
    {
        TRequest? body;

        try
        {
            body = await request.ReadFromJsonAsync<TRequest>(JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return (default, ApiProblemDetails.InvalidRequestBody(CreateController(request)));
        }

        if (body is null)
        {
            return (default, ApiProblemDetails.InvalidRequestBody(CreateController(request)));
        }

        var validation = await validator.ValidateAsync(body, cancellationToken);
        if (!validation.IsValid)
        {
            return (default, ApiProblemDetails.Validation(CreateController(request), validation.Errors));
        }

        return (body, null);
    }
}
