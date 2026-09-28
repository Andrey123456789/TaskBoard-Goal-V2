using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace TaskBoard.IntegrationTests.Support;

/// <summary>
/// Serilog sink registered in the test host's services; the API picks it up through
/// <c>ReadFrom.Services</c>, so tests can assert on what was logged server-side.
/// </summary>
internal sealed class CapturingLogSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _events = new();

    public IReadOnlyCollection<LogEvent> Events => [.. _events];

    public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);
}
