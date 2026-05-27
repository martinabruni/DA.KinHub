using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Kin.KinHub.Shared.Api.Common.Mcp;

[McpServerToolType]
public sealed class FamilyMcpTools : McpToolBase
{
    private readonly IFamilyService _familyService;
    private readonly IKinHubServiceService _kinHubServiceService;
    private readonly IRequestValidator<CreateFamilyRequest> _createFamilyValidator;
    private readonly IRequestValidator<AddFamilyMemberRequest> _addFamilyMemberValidator;
    private readonly IRequestValidator<UpdateFamilyMemberRequest> _updateFamilyMemberValidator;
    private readonly IRequestValidator<UpdateFamilyRequest> _updateFamilyValidator;
    private readonly IRequestValidator<ToggleFamilyServiceRequest> _toggleFamilyServiceValidator;

    public FamilyMcpTools(
        ICurrentUser currentUser,
        IFamilyService familyService,
        IKinHubServiceService kinHubServiceService,
        IRequestValidator<CreateFamilyRequest> createFamilyValidator,
        IRequestValidator<AddFamilyMemberRequest> addFamilyMemberValidator,
        IRequestValidator<UpdateFamilyMemberRequest> updateFamilyMemberValidator,
        IRequestValidator<UpdateFamilyRequest> updateFamilyValidator,
        IRequestValidator<ToggleFamilyServiceRequest> toggleFamilyServiceValidator)
        : base(currentUser)
    {
        _familyService = familyService;
        _kinHubServiceService = kinHubServiceService;
        _createFamilyValidator = createFamilyValidator;
        _addFamilyMemberValidator = addFamilyMemberValidator;
        _updateFamilyMemberValidator = updateFamilyMemberValidator;
        _updateFamilyValidator = updateFamilyValidator;
        _toggleFamilyServiceValidator = toggleFamilyServiceValidator;
    }

    [Authorize]
    [McpServerTool(Name = "family.get"), Description("Get the current family.")]
    public async Task<CallToolResult> GetFamilyAsync(CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _familyService.GetFamilyAsync(CurrentUser.UserId, cancellationToken));

    [Authorize]
    [McpServerTool(Name = "family.create"), Description("Create a family.")]
    public Task<CallToolResult> CreateFamilyAsync(
        [Description("The family creation payload.")] CreateFamilyRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _createFamilyValidator,
            async (payload, ct) => await _familyService.CreateFamilyAsync(payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [McpServerTool(Name = "family.update"), Description("Update a family.")]
    public Task<CallToolResult> UpdateFamilyAsync(
        [Description("The target family id.")] Guid familyId,
        [Description("The family update payload.")] UpdateFamilyRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _updateFamilyValidator,
            async (payload, ct) => await _familyService.UpdateFamilyAsync(familyId, payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [McpServerTool(Name = "family.delete"), Description("Delete a family.")]
    public async Task<CallToolResult> DeleteFamilyAsync(
        [Description("The target family id.")] Guid familyId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _familyService.DeleteFamilyAsync(familyId, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [McpServerTool(Name = "family.member.add"), Description("Add a family member.")]
    public Task<CallToolResult> AddFamilyMemberAsync(
        [Description("The target family id.")] Guid familyId,
        [Description("The member creation payload.")] AddFamilyMemberRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _addFamilyMemberValidator,
            async (payload, ct) => await _familyService.AddFamilyMemberAsync(familyId, payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [McpServerTool(Name = "family.member.update"), Description("Update a family member.")]
    public Task<CallToolResult> UpdateFamilyMemberAsync(
        [Description("The target family id.")] Guid familyId,
        [Description("The target member id.")] Guid memberId,
        [Description("The member update payload.")] UpdateFamilyMemberRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _updateFamilyMemberValidator,
            async (payload, ct) => await _familyService.UpdateFamilyMemberAsync(familyId, memberId, payload, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [McpServerTool(Name = "family.member.delete"), Description("Delete a family member.")]
    public async Task<CallToolResult> DeleteFamilyMemberAsync(
        [Description("The target family id.")] Guid familyId,
        [Description("The target member id.")] Guid memberId,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _familyService.DeleteFamilyMemberAsync(familyId, memberId, CurrentUser.UserId, cancellationToken));

    [Authorize]
    [McpServerTool(Name = "family.services.list"), Description("List all KinHub services.")]
    public async Task<CallToolResult> ListServicesAsync(CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _kinHubServiceService.GetAllServicesAsync(cancellationToken));

    [Authorize]
    [McpServerTool(Name = "family.services.get"), Description("Get the enabled services for a family.")]
    public async Task<CallToolResult> GetFamilyServicesAsync(
        [Description("The target family id.")] Guid id,
        CancellationToken cancellationToken = default) =>
        McpErrorMapper.FromCoreResult(await _kinHubServiceService.GetFamilyServicesAsync(id, cancellationToken));

    [Authorize]
    [McpServerTool(Name = "family.services.toggle"), Description("Enable or disable a KinHub service for a family.")]
    public Task<CallToolResult> ToggleFamilyServiceAsync(
        [Description("The target family id.")] Guid familyId,
        [Description("The payload describing the service toggle.")] ToggleFamilyServiceRequest? request = null,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreValidatedAsync(
            request,
            _toggleFamilyServiceValidator,
            async (payload, ct) => await _kinHubServiceService.ToggleFamilyServiceAsync(familyId, payload, ct),
            cancellationToken);
}
