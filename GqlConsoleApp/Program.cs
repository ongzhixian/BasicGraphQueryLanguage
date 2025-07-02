using GqlConsoleApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


// SERVICE REGISTRATION

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

// Register configuration
services.AddOptions();
services.AddSingleton<IConfiguration>(provider =>
{
    var builder = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    return builder.Build();
});

services.AddTransient<LoggingHandler>();
//services.AddTransient<IHttpMessageHandlerBuilderFilter, DefaultCredentialsHandlerFilter>();
//services.AddTransient<IHttpMessageHandlerBuilderFilter, LoggingHandlerBuilderFilter>();
//services.AddHttpClient();

// Approximate equivalent of the above 4 lines.
// This is the correct way to ConfigurePrimaryHttpMessageHandler for all HttpClient instances (instantiated by HttpClientFactory)
services.AddHttpClient().ConfigureHttpClientDefaults(httpClientBuilder =>
{

    var configuration = httpClientBuilder.Services.BuildServiceProvider().GetRequiredService<IConfiguration>();
    var authorizationTokenClientConfiguration = 
        configuration
        .GetSection("authorizationTokenClient")
        .Get<AuthorizationTokenClientConfiguration>()
        ?? throw new ArgumentNullException("authorizationTokenClient is not configured.");

    if (authorizationTokenClientConfiguration.Credentials == null)
        throw new ArgumentNullException("authorizationTokenClient.credentials is not configured.");

    httpClientBuilder.AddDefaultLogger();
    httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseDefaultCredentials = authorizationTokenClientConfiguration.Credentials.UseDefaultCredential,
        Credentials = authorizationTokenClientConfiguration.Credentials.ToNetworkCredential()
    });

    //httpClientBuilder.ConfigureAdditionalHttpMessageHandlers((handlers, serviceProvider) =>
    //{
    //    handlers.Add(serviceProvider.GetRequiredService<LoggingHandler>());
    //});
});

services.AddScoped<CountriesDemo>();
services.AddScoped<AriadneDemo>();


services.AddSingleton<IServerSentEventParser, ServerSentEventParser>();
services.AddSingleton<IServerSentEventHandler, ServerSentEventHandlerForConsole>();
services.AddSingleton<IGraphQlClient, GraphQlClient>();
services.AddSingleton<IAuthorizationTokenClient, AuthorizationTokenClient>();

// MAIN

IServiceProvider serviceProvider = services.BuildServiceProvider();

Console.WriteLine("Welcome to the GraphQL Console App!");

//await serviceProvider.GetRequiredService<CountriesDemo>().RunGraphQLQueryAsync();
//await serviceProvider.GetRequiredService<AriadneDemo>().RunGraphQLQueryAsync();
//await serviceProvider.GetRequiredService<AriadneDemo>().RunGraphQLSubscriptionAsync(); // Not tested/working
//await serviceProvider.GetRequiredService<AriadneDemo>().RunGraphQLSubscriptionSseAsync();

var tokenClient = serviceProvider.GetRequiredService<IAuthorizationTokenClient>();
var result = await tokenClient.TestGetTokenAsync();
var csidJwt = await tokenClient.GetAuthorizationTokenAsync("CoreData");
Console.WriteLine("CSID (JWT): {0}", csidJwt);

// await DoWorkAsync();


async Task DoWorkAsync()
{
    var _parser = serviceProvider.GetRequiredService<IServerSentEventParser>();
    var _handler = serviceProvider.GetRequiredService<IServerSentEventHandler>();
    var response = await serviceProvider.GetRequiredService<IGraphQlClient>().SubscribeAsync(
        "http://localhost:9400",
        @"
subscription { 
    position {
        userId
        position
    }
}");
    using var stream = await response.Content.ReadAsStreamAsync();
    using var reader = new StreamReader(stream);

    var eventLines = new List<string>();
    string? line;
    while ((line = await reader.ReadLineAsync()) != null)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            if (eventLines.Count > 0)
            {
                var sseEvent = _parser.Parse(eventLines);
                await _handler.HandleAsync(sseEvent);
                eventLines.Clear();
            }
            continue;
        }
        eventLines.Add(line);
    }
}
