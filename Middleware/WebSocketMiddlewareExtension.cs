using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
namespace WebServer.Middleware
{
    public static class WebSocketMiddlewareExtension
    {
        public static IApplicationBuilder UseWebSocketServer(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<WebSocketMiddleware>();
        }

        public static IServiceCollection AddWebSocketConnectionManager(this IServiceCollection services)
        {
            services.AddSingleton<WebSocketConnectionManager>();
            return services;
        }
    }
}