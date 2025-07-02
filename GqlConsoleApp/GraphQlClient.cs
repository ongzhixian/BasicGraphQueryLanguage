using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GqlConsoleApp;

public interface IGraphQlClient
{
    Task<HttpResponseMessage> SubscribeAsync(string endpoint, string query, CancellationToken cancellationToken = default);
}


public class GraphQlClient : IGraphQlClient
{
    private readonly IServerSentEventParser _sseParser;
    private readonly IServerSentEventHandler _sseHandler;
    private readonly IHttpClientFactory _httpClientFactory;

    public GraphQlClient(IServerSentEventParser sseParser, IServerSentEventHandler sseHandler, IHttpClientFactory httpClientFactory)
    {
        _sseParser = sseParser ?? throw new ArgumentNullException(nameof(sseParser));
        _sseHandler = sseHandler ?? throw new ArgumentNullException(nameof(sseHandler));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task<HttpResponseMessage> SubscribeAsync(string endpoint, string query, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Content = new StringContent(JsonSerializer.Serialize(new { query }), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        return response;
    }



    //public async Task HandleServerSentEventsAsync(Stream stream, CancellationToken cancellationToken = default)
    //{
    //    using var reader = new StreamReader(stream);
    //    var eventLines = new List<string>();
        
    //    while (!cancellationToken.IsCancellationRequested)
    //    {
    //        var line = await reader.ReadLineAsync();
    //        if (line == null) break; // End of stream
    //        if (string.IsNullOrWhiteSpace(line))
    //        {
    //            if (eventLines.Count > 0)
    //            {
    //                var sseEvent = _sseParser.Parse(eventLines);
    //                await _sseHandler.HandleAsync(sseEvent, cancellationToken);
    //                eventLines.Clear();
    //            }
    //        }
    //        else
    //        {
    //            eventLines.Add(line);
    //        }
    //    }
        
    //    // Handle any remaining lines as a final event
    //    if (eventLines.Count > 0)
    //    {
    //        var sseEvent = _sseParser.Parse(eventLines);
    //        await _sseHandler.HandleAsync(sseEvent, cancellationToken);
    //    }
    //}
}
