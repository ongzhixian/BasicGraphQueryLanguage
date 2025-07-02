using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace GqlConsoleApp;

public class LoggingHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
{
    private readonly IServiceProvider _serviceProvider;

    public LoggingHandlerBuilderFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        return builder =>
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<LoggingHandler>>();
            builder.AdditionalHandlers.Add(new LoggingHandler(logger));
            next(builder);
        };
    }
}


public class DefaultCredentialsHandlerFilter : IHttpMessageHandlerBuilderFilter
{
    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        return builder =>
        {
            switch (builder.PrimaryHandler)
            {
                case HttpClientHandler httpClientHandler:
                    httpClientHandler.UseDefaultCredentials = true;
                    break;
                case SocketsHttpHandler socketsHandler:
                    // SocketsHttpHandler does not support UseDefaultCredentials.
                    // Instead, you can set the Credentials property to use default credentials.
                    socketsHandler.Credentials = System.Net.CredentialCache.DefaultCredentials;

                    // If we want to use default credentials for proxy authentication, we can set it like this:
                    //socketsHandler.DefaultProxyCredentials = System.Net.CredentialCache.DefaultCredentials;
                    break;
            }

            next(builder);
        };
    }
}


//public class DefaultCredentialsHandlerFilter : IHttpMessageHandlerBuilderFilter
//{
//    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
//    {
//        return builder =>
//        {
//            // Force HttpClientHandler as the primary handler
//            builder.PrimaryHandler = new HttpClientHandler
//            {
//                UseDefaultCredentials = true
//            };
//            next(builder);
//        };
//    }
//}
