namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public sealed class IdentityAccountService : IIdentityAccountService
{
    private readonly IRegisterUserHandler _registerUserHandler;
    private readonly IGetCurrentUserHandler _getCurrentUserHandler;
    private readonly IUpdateUserEmailHandler _updateUserEmailHandler;
    private readonly IUpdateUserPasswordHandler _updateUserPasswordHandler;
    private readonly IDeleteUserHandler _deleteUserHandler;
    private readonly IUserProviderService _userProviderService;

    public IdentityAccountService(
        IRegisterUserHandler registerUserHandler,
        IGetCurrentUserHandler getCurrentUserHandler,
        IUpdateUserEmailHandler updateUserEmailHandler,
        IUpdateUserPasswordHandler updateUserPasswordHandler,
        IDeleteUserHandler deleteUserHandler,
        IUserProviderService userProviderService)
    {
        _registerUserHandler = registerUserHandler;
        _getCurrentUserHandler = getCurrentUserHandler;
        _updateUserEmailHandler = updateUserEmailHandler;
        _updateUserPasswordHandler = updateUserPasswordHandler;
        _deleteUserHandler = deleteUserHandler;
        _userProviderService = userProviderService;
    }

    public Task<Result<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default) =>
        _registerUserHandler.HandleAsync(request, cancellationToken);

    public Task<Result<UserProfileResponse>> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _getCurrentUserHandler.HandleAsync(userId, cancellationToken);

    public Task<Result<bool>> UpdateEmailAsync(
        Guid userId,
        UpdateUserEmailRequest request,
        CancellationToken cancellationToken = default) =>
        _updateUserEmailHandler.HandleAsync(userId, request, cancellationToken);

    public Task<Result<bool>> UpdatePasswordAsync(
        Guid userId,
        UpdateUserPasswordRequest request,
        CancellationToken cancellationToken = default) =>
        _updateUserPasswordHandler.HandleAsync(userId, request, cancellationToken);

    public Task<Result<bool>> DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _deleteUserHandler.HandleAsync(userId, cancellationToken);

    public Task<Result<IReadOnlyList<LinkedProviderResponse>>> GetProvidersAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _userProviderService.GetProvidersAsync(userId, cancellationToken);

    public Task<Result<IReadOnlyList<LinkedProviderResponse>>> LinkProviderAsync(
        Guid userId,
        LinkProviderRequest request,
        CancellationToken cancellationToken = default) =>
        _userProviderService.LinkAsync(userId, request, cancellationToken);

    public Task<Result<IReadOnlyList<LinkedProviderResponse>>> UnlinkProviderAsync(
        Guid userId,
        IdentityProviderType provider,
        CancellationToken cancellationToken = default) =>
        _userProviderService.UnlinkAsync(userId, provider, cancellationToken);
}
