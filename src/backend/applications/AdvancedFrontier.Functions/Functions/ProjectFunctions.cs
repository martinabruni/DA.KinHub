using AdvancedFrontier.Business.Common;
using AdvancedFrontier.Business.Projects;
using AdvancedFrontier.Functions.Configuration;
using AdvancedFrontier.Functions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AdvancedFrontier.Functions.Functions;

public sealed class ProjectFunctions(IProjectService projectService, ApiAuthorization authorization)
{
    [Function("ListProjects")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/projects")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        ApiResults.ApplyCorrelationId(request);
        if (!await authorization.AuthorizeAsync(request.HttpContext))
        {
            return ApiResults.Problem(request, StatusCodes.Status401Unauthorized, "Unauthorized", "A valid KinHub API token is required.", "auth.required");
        }

        return new OkObjectResult(await projectService.ListAsync(cancellationToken));
    }

    [Function("CreateProject")]
    public async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/projects")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        ApiResults.ApplyCorrelationId(request);
        if (!await authorization.AuthorizeAsync(request.HttpContext))
        {
            return ApiResults.Problem(request, StatusCodes.Status401Unauthorized, "Unauthorized", "A valid KinHub API token is required.", "auth.required");
        }

        CreateProjectRequest? command;
        try
        {
            command = await request.ReadFromJsonAsync<CreateProjectRequest>(cancellationToken);
        }
        catch (System.Text.Json.JsonException)
        {
            return ApiResults.Problem(request, StatusCodes.Status400BadRequest, "Invalid request", "The JSON body is not valid.", "request.invalidJson");
        }

        if (command is null)
        {
            return ApiResults.Problem(request, StatusCodes.Status400BadRequest, "Invalid request", "A request body is required.", "request.bodyRequired");
        }

        try
        {
            var project = await projectService.CreateAsync(command, cancellationToken);
            return new ObjectResult(project) { StatusCode = StatusCodes.Status201Created };
        }
        catch (BusinessValidationException exception)
        {
            return ApiResults.Problem(request, StatusCodes.Status400BadRequest, "Validation failed", exception.Message, exception.Code);
        }
    }
}
