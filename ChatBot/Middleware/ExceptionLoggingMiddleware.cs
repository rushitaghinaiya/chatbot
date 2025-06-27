using ChatBot.Models.Common;
using ChatBot.Models.Entities;
using ChatBot.Repository;

namespace ChatBot.Middleware
{
    public class ExceptionLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ExceptionLogRepository _logger;

        public ExceptionLoggingMiddleware(RequestDelegate next, ExceptionLogRepository logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var log = new ExceptionLog
                {
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    ExceptionType = ex.GetType().FullName,
                    Path = context.Request.Path,
                    Method = context.Request.Method,
                    StatusCode = 500,
                    Timestamp = DateTime.UtcNow,
                    User = context.User?.Identity?.Name
                };
                _logger.Log(log);
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("An internal error occurred.");
            }
        }
    }
}
