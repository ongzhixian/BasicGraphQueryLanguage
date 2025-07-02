using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace GqlConsoleApp;

public interface IAuthorizationTokenClient
{
    Task<string> GetAuthorizationTokenAsync(string applicationName);
    Task<string?> TestGetTokenAsync();
}

public class AuthorizationTokenClient : IAuthorizationTokenClient
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly AuthorizationTokenClientConfiguration authorizationTokenClientConfiguration;

    public AuthorizationTokenClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        this.httpClientFactory = httpClientFactory;

        authorizationTokenClientConfiguration =
            configuration
            .GetSection("authorizationTokenClient")
            .Get<AuthorizationTokenClientConfiguration>()
            ?? throw new ArgumentNullException("authorizationTokenClient is not configured.");
    }

    public async Task<string> GetAuthorizationTokenAsync(string applicationName)
    {
        var endpointUrl = $"{authorizationTokenClientConfiguration.EndpointUrl}?applications={applicationName}";
        
        using var request = new HttpRequestMessage(HttpMethod.Get, endpointUrl);

        using var httpClient = httpClientFactory.CreateClient();

        using var httpResponse = await httpClient.SendAsync(request);

        var csIdTokenResponse = await httpResponse.Content.ReadFromJsonAsync<CsIdTokenResponse>();

        await File.WriteAllTextAsync("csid_token2.txt", csIdTokenResponse?.token);

        return csIdTokenResponse?.token ?? string.Empty;

        //var json = await httpResponse.Content.ReadAsStringAsync();
        //return json;
    }

    public async Task<string?> TestGetTokenAsync()
    {

        Console.WriteLine("Retrieving CSID token from API...");
        var csidTokenUrl = "https://cs-identity.mlp.com/api/v2.0/token?applications=CoreData";

        // Custom certificate validation callback (optional, see note below)
        var handler = new HttpClientHandler
        {
            UseDefaultCredentials = true, // Negotiate auth
            //ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            //{
            //    // TODO: Implement custom CA validation if needed
            //    // For now, accept if no errors
            //    return errors == SslPolicyErrors.None;
            //}
        };

        using var client = new HttpClient(handler);
        var response = await client.GetAsync(csidTokenUrl);
        response.EnsureSuccessStatusCode();

        var jsonString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonString);
        var csidToken = doc.RootElement.GetProperty("token").GetString();

        await File.WriteAllTextAsync("csid_token.txt", csidToken);
        return csidToken;

    }
}


public class AuthorizationTokenClientConfiguration
{
    public string EndpointUrl { get; set; } = string.Empty;

    public Credential Credentials { get; set; }
}


public class Credential
{
    public string username { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
    public string domain { get; set; } = string.Empty;

    public bool UseDefaultCredential => string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(password);

    public ICredentials ToNetworkCredential()
    {
        if (UseDefaultCredential)
            return CredentialCache.DefaultCredentials;

        return new NetworkCredential(username, this.password, this.domain);
    }
}


public class CsIdTokenResponse
{
    public string? token { get; set; }
    
    public DateTime validUntil { get; set; }
    
    public string? user { get; set; }
}
