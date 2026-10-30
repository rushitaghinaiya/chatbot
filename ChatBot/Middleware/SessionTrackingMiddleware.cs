using ChatBot.Models.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
            string authHeader = context.Request.Headers["Authorization"];

            // ✅ If no Bearer token, skip and move to next middleware
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                await _next(context);
                return;
            }

            string token = authHeader.Substring("Bearer ".Length).Trim();
            string? emailId = null;

            // Validate token and extract userId
            if (ValidateToken(token, out string extractedUserId))
            {
                emailId = extractedUserId;
            }
            else
            {
                await _next(context);
                return;
            }

            string ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string agent = context.Request.Headers["User-Agent"];

            string sessionKey = emailId != null
                ? $"user-{emailId}"
                : $"ip-{ip}-agent-{agent}".GetHashCode().ToString();

            await sessionService.UpdateSessionAsync(emailId, sessionKey, ip, agent);

            await _next(context);
        }



        private bool ValidateToken(string token, out string emailId)
        {
            emailId = null; // default

            var jwtToken = ParseJwtToken(token);
            if (jwtToken == null)
                return false;

            // Check expiration
            var expiration = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");
            if (expiration == null)
                return false;

            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(expiration.Value));
            DateTime dateTime = dateTimeOffset.UtcDateTime;
            if (DateTime.UtcNow >= dateTime)
                return false;

            // ✅ Get userId claim
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "EmailId" || c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                emailId = userIdClaim.Value;
            }

            return true;
        }


        // Sample method to parse JWT token (you may use a library for this)
        private JwtSecurityToken ParseJwtToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ReadToken(token) as JwtSecurityToken;
        }

    }
}
