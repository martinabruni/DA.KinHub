using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using CoreResult = Kin.KinHub.Core.Business.Common;
using IdentityResult = Kin.KinHub.Identity.Business.Common;

namespace Kin.KinHub.Shared.Api.Common.Mcp;

public abstract class McpToolBase
{
    protected McpToolBase(ICurrentUser currentUser)
    {
        CurrentUser = currentUser;
    }

    protected ICurrentUser CurrentUser { get; }

    protected async Task<CallToolResult> ExecuteCoreValidatedAsync<TRequest, TResponse>(
        TRequest? request,
        IRequestValidator<TRequest> validator,
        Func<TRequest, CancellationToken, Task<CoreResult.Result<TResponse>>> action,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        if (request is null)
            throw new McpException("The request payload is required.");

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return McpErrorMapper.ToolError(validation.Errors);

        return McpErrorMapper.FromCoreResult(await action(request, cancellationToken));
    }

    protected async Task<CallToolResult> ExecuteIdentityValidatedAsync<TRequest, TResponse>(
        TRequest? request,
        IRequestValidator<TRequest> validator,
        Func<TRequest, CancellationToken, Task<IdentityResult.Result<TResponse>>> action,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        if (request is null)
            throw new McpException("The request payload is required.");

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return McpErrorMapper.ToolError(validation.Errors);

        return McpErrorMapper.FromIdentityResult(await action(request, cancellationToken));
    }

    protected static Guid RequireGuid(Guid? id, string propertyName) =>
        id ?? throw new McpException($"The '{propertyName}' argument is required.");
}
