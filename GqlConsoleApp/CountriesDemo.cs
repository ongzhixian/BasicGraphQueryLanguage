using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace GqlConsoleApp;

public class CountriesDemo
{
    private readonly ILogger<CountriesDemo> logger;
    private readonly IHttpClientFactory httpClientFactory;

    public CountriesDemo(ILogger<CountriesDemo> logger, IHttpClientFactory httpClientFactory)
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


    string endpoint = "https://countries.trevorblades.com/";
    string query = @"
{
  country(code: ""US"") {
    name
    capital
    currency
  }
}";
}
