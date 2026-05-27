namespace Kin.KinHub.Shared.Api.Common.Mcp;

internal sealed class McpSessionService : IMcpSessionService
{
    private readonly McpSessionStore _sessionStore;

    public McpSessionService(McpSessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    public McpSessionState CreateSession(McpInitializeRequestParams request) =>
        _sessionStore.Create(
            request.ProtocolVersion,
            request.ClientInfo.Name,
            request.ClientInfo.Version);

    public bool TryGetSession(string sessionId, out McpSessionState? session) =>
        _sessionStore.TryGet(sessionId, out session);

    public bool TryMarkInitialized(string sessionId, out McpSessionState? session)
    {
        if (_sessionStore.TryGet(sessionId, out session))
        {
            session!.MarkClientInitialized();
            return true;
        }

        return false;
    }

    public bool TerminateSession(string sessionId) =>
        _sessionStore.TryRemove(sessionId);
}
