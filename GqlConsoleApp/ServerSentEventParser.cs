using System;
using System.Text;
using System.Text.Json;
using GqlConsoleApp.Models;

namespace GqlConsoleApp;

public interface IServerSentEventParser
{
    ServerSentEvent Parse(IEnumerable<string> eventLines);
}


public class ServerSentEventParser : IServerSentEventParser
{
    public ServerSentEvent Parse(IEnumerable<string> eventLines)
    {
        var sseEvent = new ServerSentEvent();
        var dataBuilder = new StringBuilder();

        foreach (var line in eventLines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith(":"))
            {
                sseEvent.Comment = line.Substring(1).Trim();
                continue;
            }
            if (line.StartsWith("event:"))
            {
                sseEvent.EventType = line.Substring(6).Trim();
                continue;
            }
            if (line.StartsWith("id:"))
            {
                sseEvent.Id = line.Substring(3).Trim();
                continue;
            }
            if (line.StartsWith("data:"))
            {
                dataBuilder.AppendLine(line.Substring(5).TrimStart());
                continue;
            }
        }

        sseEvent.Data = dataBuilder.ToString().TrimEnd('\n', '\r');
        return sseEvent;
    }
}
