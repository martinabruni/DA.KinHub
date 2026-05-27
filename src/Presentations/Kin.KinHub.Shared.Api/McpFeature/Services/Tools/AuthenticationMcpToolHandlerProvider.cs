using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Kin.KinHub.Shared.Api.Common.Mcp;

[McpServerToolType]
public sealed class AuthenticationMcpTools : McpToolBase
{
    private readonly IRegisterUserHandler _registerUserHandler;
    private readonly IGetCurrentUserHandler _getCurrentUserHandler;
    private readonly IUpdateUserEmailHandler _updateUserEmailHandler;
    private readonly IUpdateUserPasswordHandler _updateUserPasswordHandler;
    private readonly IDeleteUserHandler _deleteUserHandler;
    private readonly IRequestValidator<RegisterRequest> _registerValidator;
    private readonly IRequestValidator<UpdateUserEmailRequest> _updateEmailValidator;
    private readonly IRequestValidator<UpdateUserPasswordRequest> _updatePasswordValidator;

    public AuthenticationMcpTools(
        ICurrentUser currentUser,
        IRegisterUserHandler registerUserHandler,
        IGetCurrentUserHandler getCurrentUserHandler,
        IUpdateUserEmailHandler updateUserEmailHandler,
        IUpdateUserPasswordHandler updateUserPasswordHandler,
        IDeleteUserHandler deleteUserHandler,
        IRequestValidator<RegisterRequest> registerValidator,
        IRequestValidator<UpdateUserEmailRequest> updateEmailValidator,
        IRequestValidator<UpdateUserPasswordRequest> updatePasswordValidator)
        : base(currentUser)
    {
        _registerUserHandler = registerUserHandler;
        _getCurrentUserHandler = getCurrentUserHandler;
        _updateUserEmailHandler = updateUserEmailHandler;
        _updateUserPasswordHandler = updateUserPasswordHandler;
        _deleteUserHandler = deleteUserHandler;
        _registerValidator = registerValidator;
        _updateEmailValidator = updateEmailValidator;
        _updatePasswordValidator = updatePasswordValidator;
    }

    [McpServerTool(Name = "auth.register"), Description("Register a new KinHub user.")]
    public Task<CallToolResult> RegisterAsync(
        [Description("The email for the new user.")] string email,
        [Description("The password for the new user.")] string password,
        [Description("An optional display name for the new user.")] string? displayName = null,
        CancellationToken cancellationToken = default) =>
        ExecuteIdentityValidatedAsync(
            new RegisterRequest
            {
                Email = email,
                Password = password,
                DisplayName = displayName,
            },
            _registerValidator,
            _registerUserHandler.HandleAsync,
            cancellationToken);

    [Authorize]
    [McpServerTool(Name = "auth.account.get"), Description("Read the current account.")]
    public async Task<CallToolResult> GetAccountAsync(CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromIdentityResult(await _getCurrentUserHandler.HandleAsync(CurrentUser.UserId, cancellationToken));

    [Authorize]
    [McpServerTool(Name = "auth.account.update-email"), Description("Update the current account email.")]
    public Task<CallToolResult> UpdateEmailAsync(
        [Description("The email update payload.")] UpdateUserEmailRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteIdentityValidatedAsync(
            request,
            _updateEmailValidator,
            async (payload, ct) => await _updateUserEmailHandler.HandleAsync(CurrentUser.UserId, payload, ct),
            cancellationToken);

    [Authorize]
    [McpServerTool(Name = "auth.account.update-password"), Description("Update the current account password.")]
    public Task<CallToolResult> UpdatePasswordAsync(
        [Description("The password update payload.")] UpdateUserPasswordRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteIdentityValidatedAsync(
            request,
            _updatePasswordValidator,
            async (payload, ct) => await _updateUserPasswordHandler.HandleAsync(CurrentUser.UserId, payload, ct),
            cancellationToken);

    [Authorize]
    [McpServerTool(Name = "auth.account.delete"), Description("Delete the current account.")]
    public async Task<CallToolResult> DeleteAccountAsync(CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromIdentityResult(await _deleteUserHandler.HandleAsync(CurrentUser.UserId, cancellationToken));
}
