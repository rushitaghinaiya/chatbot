using ChatBot.Models.Entities;
using ChatBot.Models.Services;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ChatBot.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string requestPath = context.Request.Path.ToString();
            string[] pathsToCheck = new string[] { "SignUp", "RefreshToken", };

            if (!pathsToCheck.Any(path => requestPath.Contains(path)))
            {
                var apiName = GetApiName(context);
                var userId = GetUserIdFromToken(context);
                var requestTime = DateTime.Now;
                var method = context.Request.Method;
                var headers = GetHeaders(context.Request);
                var queryString = context.Request.QueryString.ToString();
                // var body = await ReadRequestBody(context.Request);

                using (var scope = _scopeFactory.CreateScope())
                {
                    var apiLogService = scope.ServiceProvider.GetRequiredService<IApiLogService>();

                    var log = new ApiLog
                    {
                        ApiName = apiName,
                        UserId = userId,
                        RequestTime = requestTime,
                        RequestMethod = method,
                        RequestHeaders = headers,
                        // RequestBody = body,
                        QueryString = queryString
                    };

                    // Log the request details
                    await apiLogService.LogAsync(log);
                }

                _logger.LogInformation($"API Request: Name={apiName}, UserId={userId}, Time={requestTime}, Method={method}, Headers={headers}, QueryString={queryString}");
            }
            await _next(context);
        }
        private string GetApiName(HttpContext context)
        {
            // Split the endpoint path by '/'
            string[] parts = context.Request.Path.ToString().Replace("/api/v1/", "").Split('/');

            return $"{parts[0]}/{parts[1]}";
        }
        private string GetUserIdFromToken(HttpContext context)
        {
            // Get the Authorization header
            string authHeader = context.Request.Headers["Authorization"];

            // ✅ If no Bearer token, skip and move to next middleware
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return "0";
            }

            // Extract the token
            string token = authHeader.Substring("Bearer ".Length).Trim();

            // Parse the JWT token
            var jwtToken = ParseJwtToken(token);
            if (jwtToken == null)
            {
                return "0";
            }

            // ✅ Check if token is expired
            var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");
            if (expClaim == null || !long.TryParse(expClaim.Value, out long expValue))
            {
                return "0";
            }

            DateTime tokenExpiry = DateTimeOffset.FromUnixTimeSeconds(expValue).UtcDateTime;
            if (DateTime.UtcNow >= tokenExpiry)
            {
                return "0";
            }

            // ✅ Retrieve the UserId claim (check both standard and custom claims)
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c =>
                c.Type == "UserId" || c.Type == ClaimTypes.NameIdentifier);

            return userIdClaim?.Value ?? "0";
        }

        private JwtSecurityToken ParseJwtToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ReadToken(token) as JwtSecurityToken;
        }
        private static async Task<string> ReadRequestBody(HttpRequest request)
        {
            request.EnableBuffering();
            var bodyStream = new StreamReader(request.Body, Encoding.UTF8);
            var body = await bodyStream.ReadToEndAsync();
            request.Body.Position = 0;
            return body;
        }

        private static string GetHeaders(HttpRequest request)
        {
            var headers = request.Headers.ToDictionary(header => header.Key, header => string.Join(",", header.Value));
            return JsonConvert.SerializeObject(headers);
        }
    }
}
