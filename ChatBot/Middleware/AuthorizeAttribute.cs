using ChatBot.Models.Common;
using ChatBot.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ChatBot.Models.Services;

namespace ChatBot.Middleware
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private AppSettings _appSetting;
        public IUser _userService;
        public IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AuthorizeAttribute(IUser userService, IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> appSettings, IConfiguration configuration)
        {
            _appSetting = appSettings.Value;
            _userService = userService;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            //ResponseVM responseVM = new ResponseVM();
            // Get the Authorization header from the request
            var authorizationHeader = context.HttpContext.Request.Headers["Authorization"].ToString();

            // skip authorization if action is decorated with [AllowAnonymous] attribute
            var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
            if (allowAnonymous)
                return;
            // Check if the Authorization header is present
            if (string.IsNullOrEmpty(authorizationHeader))
            {
                // No Authorization header, so unauthorized

                context.Result = new JsonResult(
                    new
                    {
                        statusMessage = "UnAuthorized",
                        userId = 0,
                        IsSuccess = false,
                        statusCode = (int)HttpStatusCode.Unauthorized
                    });
                return;
            }

            // Extract the token from the Authorization header (assuming it's in the form "Bearer <token>")
            var token = authorizationHeader.Split(' ')[1]; // Assumes Bearer token scheme

            // Validate the token (e.g., using JWT validation)
            bool isTokenValid = ValidateToken(token);

            if (!isTokenValid)
            {
                AuthenticationModel authenticationModel = new AuthenticationModel();
                //authenticationModel = RefreshTokenAsync(token).Result;
                if (authenticationModel.IsAuthenticated)
                {
                    context.HttpContext.Items["NewAccessToken"] = authenticationModel.Token;
                    context.HttpContext.Items["NewRefreshToken"] = authenticationModel.RefreshToken;
                    return;
                }
                if (authenticationModel.Message == "Token Not Active." || authenticationModel.Message == "Token did not match any users.")
                {
                    context.Result = new JsonResult(
                       new
                       {
                           path = context.HttpContext.Request.Path,
                           statusMessage = "Token Not Active Or Token did not match any users.",
                           userId = 0,
                           IsSuccess = false,
                           statusCode = (int)HttpStatusCode.Unauthorized
                       });
                    return;
                }
                // Token is invalid or expired
                //_httpContextAccessor.HttpContext.Session.SetString("Token",authenticationModel.Token);
                //context.Result = new JsonResult(
                //   new
                //   {
                //       statusMessage = authenticationModel.Token,
                //       userId = 0,
                //       IsSuccess = false,
                //       statusCode = (int)HttpStatusCode.Unauthorized
                //   });
                return;
            }

            // Token is valid, proceed with authorization
        }

        private bool ValidateToken(string token)
        {
            // Implement token validation logic here
            // This may involve validating JWT token signature, issuer, audience, and expiration
            // You may use libraries like System.IdentityModel.Tokens.Jwt for JWT token validation

            // For demonstration purposes, let's assume the token is valid if it's not expired
            var jwtToken = ParseJwtToken(token);
            if (jwtToken == null)
                return false;

            // Check if the token's expiration claim is in the past
            var expiration = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");
            if (expiration == null)
                return false;

            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(expiration.Value));
            DateTime dateTime = dateTimeOffset.UtcDateTime;
            return DateTime.UtcNow < dateTime;
        }

        // Sample method to parse JWT token (you may use a library for this)
        private JwtSecurityToken ParseJwtToken(string token)
        {
            // Implement JWT token parsing logic here
            // You may use libraries like System.IdentityModel.Tokens.Jwt for parsing

            // For demonstration purposes, let's assume the token is a JWT token
            // and parse it manually (in reality, you should use a library)
            var handler = new JwtSecurityTokenHandler();
            return handler.ReadToken(token) as JwtSecurityToken;
        }


       

       

    }
}
