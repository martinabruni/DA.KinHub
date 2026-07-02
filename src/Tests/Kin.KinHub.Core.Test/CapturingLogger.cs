using Microsoft.Extensions.Logging;

namespace Kin.KinHub.Core.Test;

/// <summary>
/// Minimal in-memory <see cref="ILogger{TCategoryName}"/> that captures each log entry's rendered
/// message and its structured state key/value pairs, so tests can assert exactly what was (and was
/// not) emitted.
/// </summary>
public sealed class CapturingLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = [];

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var stateValues = state is IReadOnlyList<KeyValuePair<string, object?>> pairs
            ? pairs.ToList()
            : [];

        Entries.Add(new LogEntry(
            logLevel,
            formatter(state, exception),
            stateValues,
            exception));
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}

public sealed record LogEntry(
    LogLevel Level,
    string Message,
    IReadOnlyList<KeyValuePair<string, object?>> StateValues,
    Exception? Exception);
