using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
// Add missing using directives for GraphQL.Client


namespace GqlConsoleApp;

public class AriadneDemo
{
    private readonly ILogger<AriadneDemo> logger;
    private readonly IHttpClientFactory httpClientFactory;

    public AriadneDemo(ILogger<AriadneDemo> logger, IHttpClientFactory httpClientFactory)
    {
        this.logger = logger;
        this.httpClientFactory = httpClientFactory;
    }

    public async Task RunGraphQLQueryAsync()
    {
        var requestBody = new
        {
            query = query
        };
        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using var client = this.httpClientFactory.CreateClient();
            var response = await client.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var data))
            {
                Console.WriteLine("GraphQL Response:");
                Console.WriteLine(data);
            }
            else
            {
                Console.WriteLine("No data found in response.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing GraphQL query");
        }
    }

    public async Task RunGraphQLSubscriptionAsync()
    {
        var wsEndpoint = endpoint.Replace("http://", "ws://").Replace("https://", "wss://");
        using var ws = new ClientWebSocket();
        ws.Options.AddSubProtocol("graphql-ws");

        try
        {
            await ws.ConnectAsync(new Uri(wsEndpoint), CancellationToken.None);

            // Send connection_init
            var initMessage = JsonSerializer.Serialize(new
            {
                type = "connection_init",
                payload = new { }
            });
            var initBuffer = Encoding.UTF8.GetBytes(initMessage);
            await ws.SendAsync(initBuffer, WebSocketMessageType.Text, true, CancellationToken.None);

            // Wait for connection_ack
            var ack = await ReadMessageAsync(ws);
            if (ack == null)
            {
                logger.LogError("No response from server after connection_init.");
                return;
            }
            using (var ackDoc = JsonDocument.Parse(ack))
            {
                var root = ackDoc.RootElement;
                if (!root.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "connection_ack")
                {
                    logger.LogError("Did not receive connection_ack from server. Got: {0}", ack);
                    return;
                }
            }

            // Send subscribe
            var id = Guid.NewGuid().ToString();
            var subscribeMessage = JsonSerializer.Serialize(new
            {
                id,
                type = "subscribe",
                payload = new
                {
                    query = "subscription { beanCounter }"
                }
            });
            var subscribeBuffer = Encoding.UTF8.GetBytes(subscribeMessage);
            await ws.SendAsync(subscribeBuffer, WebSocketMessageType.Text, true, CancellationToken.None);

            // Listen for subscription data
            while (ws.State == WebSocketState.Open)
            {
                var message = await ReadMessageAsync(ws);
                if (message == null)
                    break;

                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                if (root.TryGetProperty("type", out var typeProp2))
                {
                    var typeStr = typeProp2.GetString();
                    if (typeStr == "next")
                    {
                        if (root.TryGetProperty("payload", out var payload) &&
                            payload.TryGetProperty("data", out var data))
                        {
                            Console.WriteLine("GraphQL Subscription Response:");
                            Console.WriteLine(data);
                        }
                    }
                    else if (typeStr == "error")
                    {
                        logger.LogError("GraphQL subscription error: {0}", root.ToString());
                    }
                    else if (typeStr == "complete")
                    {
                        Console.WriteLine("Subscription complete.");
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing GraphQL subscription");
        }
    }

    // Helper to read a full message from the WebSocket
    private static async Task<string?> ReadMessageAsync(ClientWebSocket ws)
    {
        var buffer = new ArraySegment<byte>(new byte[4096]);
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await ws.ReceiveAsync(buffer, CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
                return null;
            ms.Write(buffer.Array!, buffer.Offset, result.Count);
        } while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(ms.ToArray());
    }
    public async Task RunGraphQLSubscriptionSseAsync()
    {
        try
        {
            using var client = this.httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
            var body = new
            {
                query = subscription
            };
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            Console.WriteLine("SSE Subscription started. Listening for events...");
            string? line;
            var sb = new StringBuilder();
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("data:"))
                {
                    var data = line.Substring(5).Trim();
                    if (!string.IsNullOrEmpty(data))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(data);
                            if (doc.RootElement.TryGetProperty("data", out var payload))
                            {
                                Console.WriteLine("GraphQL SSE Subscription Response:");
                                Console.WriteLine(payload);
                            }
                            else
                            {
                                Console.WriteLine("Received SSE event without 'data' property:");
                                Console.WriteLine(data);
                            }
                        }
                        catch (JsonException)
                        {
                            Console.WriteLine("Invalid JSON in SSE event:");
                            Console.WriteLine(data);
                        }
                    }
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    // End of event, continue
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing GraphQL SSE subscription");
        }
    }


    string endpoint = "http://localhost:9400/";
    string query = @"
    query GetHello {
        hello
    }
        ";
    string subscription = @"
    subscription GetBeanCounter {
      beanCounter
    }
        ";
}
