using System.Text.Json;
using GqlConsoleApp.Models;

namespace GqlConsoleApp;

public interface IServerSentEventHandler
{
    Task HandleAsync(ServerSentEvent sseEvent, CancellationToken cancellationToken = default);
}

public class ServerSentEventHandlerForConsole : IServerSentEventHandler
{
    public Task HandleAsync(ServerSentEvent sseEvent, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(sseEvent.EventType))
            Console.WriteLine($"[event: {sseEvent.EventType}]");

        if (!string.IsNullOrEmpty(sseEvent.Id))
            Console.WriteLine($"[id: {sseEvent.Id}]");

        if (!string.IsNullOrEmpty(sseEvent.Comment))
            Console.WriteLine($"[comment: {sseEvent.Comment}]");

        if (!string.IsNullOrEmpty(sseEvent.Data))
        {
            try
            {
                using var doc = JsonDocument.Parse(sseEvent.Data);
                Console.WriteLine(JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (JsonException)
            {
                Console.WriteLine($"Non-JSON event data: {sseEvent.Data}");
            }
        }
        return Task.CompletedTask;
    }
}