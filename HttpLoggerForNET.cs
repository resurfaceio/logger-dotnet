using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;

namespace Resurfaceio
{
    public class HttpLoggerForNET
    {
        private readonly RequestDelegate _next;
        private readonly HttpLogger _logger;

        public HttpLoggerForNET(RequestDelegate next)
        {
            _next = next;
            _logger = new HttpLogger(url:"http://localhost:4001/message", rules:"include debug\nskip_compression");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var start = DateTime.Now;
            // TODO - read request body
            await _next(context);
            var interval = DateTime.Now.Subtract(start).Ticks / TimeSpan.TicksPerMillisecond;
            // TODO - read response body
            HttpMessage.send(_logger, context.Request, context.Response, interval:interval);
        }
    }    

    public static class HttpLoggerForNETExtensions
    {
        public static IApplicationBuilder UseHttpLoggerForNET(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpLoggerForNET>();
        }
    }
}