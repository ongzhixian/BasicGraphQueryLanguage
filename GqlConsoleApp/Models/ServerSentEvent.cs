namespace GqlConsoleApp.Models;

public class ServerSentEvent
{
    public string? Id { get; set; }
    public string? EventType { get; set; }
    public string? Data { get; set; }
    public string? Comment { get; set; }
}