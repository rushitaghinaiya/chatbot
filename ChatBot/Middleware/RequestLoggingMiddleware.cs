using ChatBot.Models.Entities;
using ChatBot.Models.Services;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
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
            string[] pathsToCheck = new string[] { "UserSignUp", "well-known", "RefreshToken", "apple-app-site-association" };

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
            var authorizationHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (authorizationHeader != null && (authorizationHeader.StartsWith("Bearer ") || (authorizationHeader.StartsWith("Barier "))))
            {
                var token = authorizationHeader.Substring("Bearer ".Length).Trim();
                if (!string.IsNullOrEmpty(token))
                    token = authorizationHeader.Substring("Barier ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

                if (jwtToken != null)
                {
                    var userId = jwtToken.Claims.First(claim => claim.Type == "UserId").Value;
                    return userId;
                }
            }
            return "Anonymous";
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
