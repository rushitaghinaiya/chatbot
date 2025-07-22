using ChatBot.Models.Services;

namespace ChatBot.Middleware
{
    public class SessionTrackingMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionTrackingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IUser sessionService)
        {
            string ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string agent = context.Request.Headers["User-Agent"];

            int? userId = context.User.Identity.IsAuthenticated
                ? int.Parse(context.User.FindFirst("userId")?.Value ?? "0")
                : null;

            // Generate a unique session key: prefer userId if authenticated
            string sessionKey = userId != null
                ? $"user-{userId}"
                : $"ip-{ip}-agent-{agent}".GetHashCode().ToString(); // simple hash to keep it shorter

            await sessionService.UpdateSessionAsync(userId, sessionKey, ip, agent);

            await _next(context);
        }

    }
}
