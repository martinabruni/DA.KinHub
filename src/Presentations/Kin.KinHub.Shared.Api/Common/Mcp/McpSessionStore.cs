using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Kin.KinHub.Shared.Api.Common.Mcp;

internal sealed class McpSessionStore
{
    private readonly ConcurrentDictionary<string, McpSessionState> _sessions = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly TimeSpan _expiration;

    public McpSessionStore(McpTransportOptions options)
    {
        _expiration = TimeSpan.FromMinutes(Math.Max(1, options.SessionIdleTimeoutMinutes));
    }

    public McpSessionState Create(string protocolVersion, string? clientName, string? clientVersion)
    {
        CleanupExpired();

        var now = _timeProvider.GetUtcNow();
        var session = new McpSessionState(
            CreateSessionId(),
            protocolVersion,
            clientName,
            clientVersion,
            now,
            now);

        _sessions[session.Id] = session;
        return session;
    }

    public bool TryGet(string sessionId, out McpSessionState? session)
    {
        CleanupExpired();

        if (_sessions.TryGetValue(sessionId, out session))
        {
            session.Touch(_timeProvider.GetUtcNow());
            return true;
        }

        session = null;
        return false;
    }

    public bool TryRemove(string sessionId) =>
        _sessions.TryRemove(sessionId, out _);

    private void CleanupExpired()
    {
        var threshold = _timeProvider.GetUtcNow() - _expiration;
        foreach (var session in _sessions.Values)
        {
            if (session.LastSeenAt < threshold)
            {
                _sessions.TryRemove(session.Id, out _);
            }
        }
    }

    private static string CreateSessionId()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }
}

public sealed class McpSessionState
{
    public McpSessionState(
        string id,
        string protocolVersion,
        string? clientName,
        string? clientVersion,
        DateTimeOffset createdAt,
        DateTimeOffset lastSeenAt)
    {
        Id = id;
        ProtocolVersion = protocolVersion;
        ClientName = clientName;
        ClientVersion = clientVersion;
        CreatedAt = createdAt;
        LastSeenAt = lastSeenAt;
    }

    public string Id { get; }
    public string ProtocolVersion { get; }
    public string? ClientName { get; }
    public string? ClientVersion { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset LastSeenAt { get; private set; }
    public bool ClientInitialized { get; private set; }

    public void MarkClientInitialized() =>
        ClientInitialized = true;

    public void Touch(DateTimeOffset now) =>
        LastSeenAt = now;
}
