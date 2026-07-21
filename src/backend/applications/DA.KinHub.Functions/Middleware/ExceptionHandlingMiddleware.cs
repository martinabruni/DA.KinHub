using DA.KinHub.Business.Common;
using DA.KinHub.Functions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace DA.KinHub.Functions.Middleware;

public sealed class ExceptionHandlingMiddleware(ApiProblemDetailsFactory problemDetailsFactory, ILogger<ExceptionHandlingMiddleware> logger) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested || context.GetHttpContext()?.RequestAborted.IsCancellationRequested == true)
        {
            throw;
        }
        catch (BusinessValidationException exception)
        {
            SetProblem(context, problemDetailsFactory.Create(GetHttpContext(context), StatusCodes.Status400BadRequest, "Invalid request", exception.Message, exception.Code));
        }
        catch (BusinessAccessDeniedException exception)
        {
            SetProblem(context, problemDetailsFactory.Create(GetHttpContext(context), StatusCodes.Status403Forbidden, "Forbidden", "Access is not allowed.", exception.Code));
        }
        catch (BusinessDependencyException exception)
        {
            logger.LogError(exception, "A required dependency failed while processing the request.");
            SetProblem(context, problemDetailsFactory.Create(GetHttpContext(context), StatusCodes.Status503ServiceUnavailable, "Service unavailable", "A required dependency is temporarily unavailable.", exception.Code));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An unexpected error occurred while processing the request.");
            SetProblem(context, problemDetailsFactory.Create(GetHttpContext(context), StatusCodes.Status500InternalServerError, "Internal server error", "The request could not be completed.", "internal.unexpected"));
        }
    }

    private static HttpContext GetHttpContext(FunctionContext context)
    {
        return context.GetHttpContext() ?? throw new InvalidOperationException("HTTP context is required to format Problem Details.");
    }

    private static void SetProblem(FunctionContext context, object result)
    {
        var httpContext = GetHttpContext(context);
        if (httpContext.Response.HasStarted)
        {
            throw new InvalidOperationException("The HTTP response has already started.");
        }

        context.GetInvocationResult().Value = result;
    }
}
