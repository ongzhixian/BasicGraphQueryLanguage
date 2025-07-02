using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace GqlConsoleApp;

public class LoggingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHandler> _logger;

    public LoggingHandler(ILogger<LoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request: {method} {url}", request.Method, request.RequestUri);

        // Log request headers
        foreach (var header in request.Headers)
        {
            _logger.LogInformation("Request Header: {header}: {values}", header.Key, string.Join(", ", header.Value));
        }

        // Log content headers if present
        if (request.Content != null)
        {
            foreach (var header in request.Content.Headers)
            {
                _logger.LogInformation("Request Content Header: {header}: {values}", header.Key, string.Join(", ", header.Value));
            }

            var requestContent = await request.Content.ReadAsStringAsync();
            _logger.LogInformation("Request Content: {content}", requestContent);
        }

        var stopwatch = Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken);
        stopwatch.Stop();

        _logger.LogInformation("Response: {statusCode} ({elapsed} ms)", (int)response.StatusCode, stopwatch.ElapsedMilliseconds);

        if (response.Content != null)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response Content: {content}", responseContent);
        }

        return response;
    }
}


