using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Shared.Api.ChatFeature;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatManager _chatManager;
    private readonly IRequestValidator<CreateConversationRequest> _createConversationValidator;
    private readonly IRequestValidator<SendMessageRequest> _sendMessageValidator;
    private readonly ICurrentUser _currentUser;

    public ChatController(
        IChatManager chatManager,
        IRequestValidator<CreateConversationRequest> createConversationValidator,
        IRequestValidator<SendMessageRequest> sendMessageValidator,
        ICurrentUser currentUser)
    {
        _chatManager = chatManager;
        _createConversationValidator = createConversationValidator;
        _sendMessageValidator = sendMessageValidator;
        _currentUser = currentUser;
    }

    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversationAsync(
        [FromBody] CreateConversationRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        if (!_currentUser.HasActiveMember)
            return BadRequest(new { message = "Missing or invalid X-Member-Id header." });

        if (request is null)
            return BadRequest(new { message = "Invalid request body." });

        var validation = await _createConversationValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors });

        var result = await _chatManager.CreateConversationAsync(_currentUser.FamilyMemberId, request.Title!);
        return HttpResultMapper.ToActionResult(result);
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversationsAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        if (!_currentUser.HasActiveMember)
            return BadRequest(new { message = "Missing or invalid X-Member-Id header." });

        var result = await _chatManager.GetConversationsAsync(_currentUser.FamilyMemberId);
        return HttpResultMapper.ToActionResult(result);
    }

    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> GetConversationAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        if (!_currentUser.HasActiveMember)
            return BadRequest(new { message = "Missing or invalid X-Member-Id header." });

        var result = await _chatManager.GetConversationAsync(id, _currentUser.FamilyMemberId);
        return HttpResultMapper.ToActionResult(result);
    }

    [HttpDelete("conversations/{id:guid}")]
    public async Task<IActionResult> DeleteConversationAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        if (!_currentUser.HasActiveMember)
            return BadRequest(new { message = "Missing or invalid X-Member-Id header." });

        var result = await _chatManager.DeleteConversationAsync(id, _currentUser.FamilyMemberId);
        return HttpResultMapper.ToActionResult(result);
    }

    [HttpPost("conversations/{id:guid}/messages")]
    public async Task<IActionResult> SendMessageAsync(
        Guid id,
        [FromBody] SendMessageRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        if (!_currentUser.HasActiveMember)
            return BadRequest(new { message = "Missing or invalid X-Member-Id header." });

        if (request is null)
            return BadRequest(new { message = "Invalid request body." });

        var validation = await _sendMessageValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors });

        var result = await _chatManager.SendMessageAsync(id, _currentUser.FamilyMemberId, request.Message!);
        return HttpResultMapper.ToActionResult(result);
    }

    [HttpPost("tool-calls/{id:guid}/confirm")]
    public async Task<IActionResult> ConfirmToolCallAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        if (!_currentUser.HasActiveMember)
            return BadRequest(new { message = "Missing or invalid X-Member-Id header." });

        var result = await _chatManager.ConfirmToolCallAsync(id, _currentUser.FamilyMemberId, _currentUser.UserId);
        return HttpResultMapper.ToActionResult(result);
    }

    [HttpPost("tool-calls/{id:guid}/reject")]
    public async Task<IActionResult> RejectToolCallAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        if (!_currentUser.HasActiveMember)
            return BadRequest(new { message = "Missing or invalid X-Member-Id header." });

        var result = await _chatManager.RejectToolCallAsync(id, _currentUser.FamilyMemberId);
        return HttpResultMapper.ToActionResult(result);
    }
}
