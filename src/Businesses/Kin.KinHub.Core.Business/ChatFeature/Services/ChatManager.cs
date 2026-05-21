namespace Kin.KinHub.Core.Business.ChatFeature;

internal sealed class ChatManager : IChatManager
{
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IChatToolCallRepository _toolCallRepository;
    private readonly IChatService _chatService;
    private readonly IChatToolExecutor _chatToolExecutor;

    public ChatManager(
        IChatConversationRepository conversationRepository,
        IChatMessageRepository messageRepository,
        IChatToolCallRepository toolCallRepository,
        IChatService chatService,
        IChatToolExecutor chatToolExecutor)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _toolCallRepository = toolCallRepository;
        _chatService = chatService;
        _chatToolExecutor = chatToolExecutor;
    }

    /// <inheritdoc/>
    public async Task<Result<ChatConversation>> CreateConversationAsync(
        Guid familyMemberId,
        string title,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var conversation = new ChatConversation
            {
                Id = Guid.NewGuid(),
                FamilyMemberId = familyMemberId,
                Title = title,
                CreatedAt = now,
                UpdatedAt = now,
            };

            var created = await _conversationRepository.CreateAsync(conversation);
            return Result<ChatConversation>.Success(created);
        }
        catch (Exception ex)
        {
            return Result<ChatConversation>.UnexpectedError(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<ChatConversation>>> GetConversationsAsync(
        Guid familyMemberId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var conversations = await _conversationRepository.GetByFamilyMemberIdAsync(familyMemberId, cancellationToken);
            return Result<IReadOnlyList<ChatConversation>>.Success(conversations);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<ChatConversation>>.UnexpectedError(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ConversationWithMessages>> GetConversationAsync(
        Guid conversationId,
        Guid familyMemberId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var conversation = await _conversationRepository.GetAsync(conversationId);

            if (conversation.FamilyMemberId != familyMemberId)
                return Result<ConversationWithMessages>.Unauthorized("Access denied to this conversation.");

            var messages = await _messageRepository.GetByConversationIdAsync(conversationId, cancellationToken);

            var pendingToolCalls = new List<ChatToolCall>();
            foreach (var message in messages.Where(m => m.Role == ChatMessageRole.Assistant))
            {
                var toolCalls = await _toolCallRepository.GetByMessageIdAsync(message.Id, cancellationToken);
                pendingToolCalls.AddRange(toolCalls.Where(tc => tc.Status == ChatToolCallStatus.Pending));
            }

            return Result<ConversationWithMessages>.Success(new ConversationWithMessages
            {
                Conversation = conversation,
                Messages = messages,
                PendingToolCalls = pendingToolCalls,
            });
        }
        catch (EntityNotFoundException ex)
        {
            return Result<ConversationWithMessages>.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<ConversationWithMessages>.UnexpectedError(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ChatConversation>> DeleteConversationAsync(
        Guid conversationId,
        Guid familyMemberId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var conversation = await _conversationRepository.GetAsync(conversationId);

            if (conversation.FamilyMemberId != familyMemberId)
                return Result<ChatConversation>.Unauthorized("Access denied to this conversation.");

            var deleted = await _conversationRepository.DeleteAsync(conversationId);
            return Result<ChatConversation>.Success(deleted);
        }
        catch (EntityNotFoundException ex)
        {
            return Result<ChatConversation>.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<ChatConversation>.UnexpectedError(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<SendMessageResponse>> SendMessageAsync(
        Guid conversationId,
        Guid familyMemberId,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var conversation = await _conversationRepository.GetAsync(conversationId);

            if (conversation.FamilyMemberId != familyMemberId)
                return Result<SendMessageResponse>.Unauthorized("Access denied to this conversation.");

            var now = DateTime.UtcNow;
            var userChatMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                Role = ChatMessageRole.User,
                Content = userMessage,
                CreatedAt = now,
                UpdatedAt = now,
            };

            await _messageRepository.CreateAsync(userChatMessage);

            var last20 = await _messageRepository.GetLastAsync(conversationId, 20, cancellationToken);
            var response = await _chatService.SendAsync(last20, cancellationToken);

            var assistantMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                Role = ChatMessageRole.Assistant,
                Content = response.TextContent ?? string.Empty,
                CreatedAt = now,
                UpdatedAt = now,
            };

            var persistedAssistant = await _messageRepository.CreateAsync(assistantMessage);
            await TouchConversationAsync(conversation, now);

            if (response.IsToolCall)
            {
                var toolCall = new ChatToolCall
                {
                    Id = Guid.NewGuid(),
                    MessageId = persistedAssistant.Id,
                    ToolName = response.ToolCallRequest!.ToolName,
                    ArgumentsJson = response.ToolCallRequest.ArgumentsJson,
                    Status = ChatToolCallStatus.Pending,
                    CreatedAt = now,
                    UpdatedAt = now,
                };

                var persistedToolCall = await _toolCallRepository.CreateAsync(toolCall);

                return Result<SendMessageResponse>.Success(new SendMessageResponse
                {
                    AssistantMessage = persistedAssistant,
                    PendingToolCall = persistedToolCall,
                });
            }

            return Result<SendMessageResponse>.Success(new SendMessageResponse
            {
                AssistantMessage = persistedAssistant,
            });
        }
        catch (EntityNotFoundException ex)
        {
            return Result<SendMessageResponse>.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<SendMessageResponse>.UnexpectedError(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ChatToolCall>> ConfirmToolCallAsync(
        Guid toolCallId,
        Guid familyMemberId,
        Guid userId,
        CancellationToken cancellationToken = default)
        => await ConfirmAndExecuteToolCallAsync(
            toolCallId,
            familyMemberId,
            userId,
            cancellationToken);

    /// <inheritdoc/>
    public async Task<Result<ChatToolCall>> RejectToolCallAsync(
        Guid toolCallId,
        Guid familyMemberId,
        CancellationToken cancellationToken = default)
        => await UpdateToolCallStatusAsync(
            toolCallId,
            familyMemberId,
            ChatToolCallStatus.Rejected,
            cancellationToken);

    private async Task<Result<ChatToolCall>> UpdateToolCallStatusAsync(
        Guid toolCallId,
        Guid familyMemberId,
        ChatToolCallStatus newStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            var toolCall = await _toolCallRepository.GetAsync(toolCallId);
            var message = await _messageRepository.GetAsync(toolCall.MessageId);
            var conversation = await _conversationRepository.GetAsync(message.ConversationId);

            if (conversation.FamilyMemberId != familyMemberId)
                return Result<ChatToolCall>.Unauthorized("Access denied to this tool call.");

            if (toolCall.Status != ChatToolCallStatus.Pending)
                return Result<ChatToolCall>.Conflict("Tool call is not in Pending status.");

            toolCall.Status = newStatus;
            toolCall.UpdatedAt = DateTime.UtcNow;

            var updated = await _toolCallRepository.UpdateAsync(toolCallId, toolCall);
            return Result<ChatToolCall>.Success(updated);
        }
        catch (EntityNotFoundException ex)
        {
            return Result<ChatToolCall>.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<ChatToolCall>.UnexpectedError(ex.Message);
        }
    }

    private async Task<Result<ChatToolCall>> ConfirmAndExecuteToolCallAsync(
        Guid toolCallId,
        Guid familyMemberId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var toolCall = await _toolCallRepository.GetAsync(toolCallId);
            var parentMessage = await _messageRepository.GetAsync(toolCall.MessageId);
            var conversation = await _conversationRepository.GetAsync(parentMessage.ConversationId);

            if (conversation.FamilyMemberId != familyMemberId)
                return Result<ChatToolCall>.Unauthorized("Access denied to this tool call.");

            if (toolCall.Status != ChatToolCallStatus.Pending)
                return Result<ChatToolCall>.Conflict("Tool call is not in Pending status.");

            toolCall.Status = ChatToolCallStatus.Confirmed;
            toolCall.UpdatedAt = DateTime.UtcNow;

            var updatedToolCall = await _toolCallRepository.UpdateAsync(toolCallId, toolCall);
            var executionResult = await _chatToolExecutor.ExecuteAsync(updatedToolCall, userId, cancellationToken);
            if (!executionResult.IsSuccess || executionResult.Value is null)
            {
                await PersistAssistantMessageAsync(
                    conversation.Id,
                    executionResult.Message ?? "The requested action could not be completed.",
                    cancellationToken);
                await TouchConversationAsync(conversation, DateTime.UtcNow);
                return Result<ChatToolCall>.Success(updatedToolCall);
            }

            await PersistAssistantMessageAsync(
                conversation.Id,
                executionResult.Value.MessageContent,
                cancellationToken);
            await TouchConversationAsync(conversation, DateTime.UtcNow);
            return Result<ChatToolCall>.Success(updatedToolCall);
        }
        catch (EntityNotFoundException ex)
        {
            return Result<ChatToolCall>.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<ChatToolCall>.UnexpectedError(ex.Message);
        }
    }

    private async Task PersistAssistantMessageAsync(
        Guid conversationId,
        string content,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        var now = DateTime.UtcNow;
        await _messageRepository.CreateAsync(new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = ChatMessageRole.Assistant,
            Content = content,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    private async Task TouchConversationAsync(ChatConversation conversation, DateTime updatedAt)
    {
        conversation.UpdatedAt = updatedAt;
        await _conversationRepository.UpdateAsync(conversation.Id, conversation);
    }

}
