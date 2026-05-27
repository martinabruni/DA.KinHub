namespace Kin.KinHub.Shared.Api.Common.Mcp;

public interface IMcpSessionService
{
    McpSessionState CreateSession(McpInitializeRequestParams request);
    bool TryGetSession(string sessionId, out McpSessionState? session);
    bool TryMarkInitialized(string sessionId, out McpSessionState? session);
    bool TerminateSession(string sessionId);
}
