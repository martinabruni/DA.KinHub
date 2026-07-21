using DA.KinHub.Functions.Configuration;
using DA.KinHub.Functions.Http;
using DA.KinHub.Functions.OpenApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using DA.KinHub.Functions.Security;

namespace DA.KinHub.Functions.Functions;

public sealed class MetadataFunctions(
    BuildInfoProvider buildInfoProvider,
    TimeProvider timeProvider,
    OpenApiDocumentProvider openApiDocumentProvider)
{
    [AllowAnonymous]
    [Function("Version")]
    public IActionResult Version([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Metadata.Version)] HttpRequest request)
    {
        ApiResults.ApplyNoStore(request.HttpContext.Response);
        return new OkObjectResult(buildInfoProvider.Get());
    }

    [AllowAnonymous]
    [Function("Status")]
    public IActionResult Status([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Metadata.Status)] HttpRequest request)
    {
        ApiResults.ApplyNoStore(request.HttpContext.Response);
        var build = buildInfoProvider.Get();
        return new OkObjectResult(new
        {
            status = "operational",
            appName = build.AppName,
            version = build.Version,
            environment = build.Environment,
            timestamp = timeProvider.GetUtcNow()
        });
    }

    [AllowAnonymous]
    [Function("OpenApi")]
    public IActionResult OpenApi([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Metadata.OpenApi)] HttpRequest request)
    {
        return new OkObjectResult(openApiDocumentProvider.GetDocument());
    }
}
