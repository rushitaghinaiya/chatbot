using ChatBot.Models.Common;
using ChatBot.Models.Configuration;
using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ChatBot.Repository
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly AppSettings _jwtConfig;
        private readonly ILogger<JwtTokenService> _logger;
        private readonly IUser _userService;

        public JwtTokenService(IOptions<AppSettings> jwtConfig, ILogger<JwtTokenService> logger, IUser userService)
        {
            _jwtConfig = jwtConfig.Value;
            _logger = logger;
            _userService = userService;
        }

        public AuthenticationModel Authenticate(int userId )
        {
            AuthenticationModel response = new AuthenticationModel();
            if (userId>0)
            {

                var userDetail = _userService.GetUserList().Where(a=>a.Id==userId).FirstOrDefault();
                if (userDetail != null)
                {
                    string jwtToken = GenerateAccessToken(userDetail);
                    response.UserName = userDetail.Name;
                    response.Token = jwtToken;
                    response.Email = userDetail.Email;
                    response.IsAuthenticated = true;
                    var refreshToken = _userService.GetRefreshTokenByUserId(userId);


                    if (refreshToken.Any(a => a.IsActive))
                    {
                        var activeRefreshToken = refreshToken.Where(a => a.IsActive == true).FirstOrDefault();
                        response.RefreshToken = activeRefreshToken.Token;
                        response.RefreshTokenExpiration = activeRefreshToken.Expires;
                    }
                    else
                    {
                        var newrefreshToken = CreateRefreshToken();
                        newrefreshToken.JWTToken= jwtToken;
                        response.RefreshToken = newrefreshToken.Token;
                        response.RefreshTokenExpiration = newrefreshToken.Expires;
                        newrefreshToken.UserId = userId;
                        _userService.SaveRefreshToken(newrefreshToken);
                    }
                    RefreshToken refreshToken1 = new RefreshToken();
                    refreshToken1.JWTToken = jwtToken;
                    refreshToken1.Token = response.RefreshToken;
                    refreshToken1.Expires = response.RefreshTokenExpiration;
                    _userService.UpdateRefreshToken(refreshToken1);
                    return response;
                }
                else
                {
                    response.Message = "Invalid Credential";
                    return response;
                }
            }
            else
            {
                response.Message = "Enter Credential";
                return response;
            }
        }
        public string GenerateAccessToken(Users user)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                    new Claim("mobile", user.Mobile ?? string.Empty),
                    new Claim("name", user.Name ?? string.Empty),
                    new Claim("role", user.Role ?? "user"),
                    new Claim("isPremium", user.IsPremium.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat,
                        new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                        ClaimValueTypes.Integer64)
                };

                var token = new JwtSecurityToken(
                    issuer: _jwtConfig.Issuer,
                    audience: _jwtConfig.Audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(_jwtConfig.ExpirationInMinutes),
                    signingCredentials: credentials
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                _logger.LogInformation("JWT token generated successfully for user ID: {UserId}", user.Id);

                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user ID: {UserId}", user.Id);
                throw;
            }
        }

        public string GenerateRefreshToken()
        {
            try
            {
                var randomNumber = new byte[64];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(randomNumber);

                var refreshToken = Convert.ToBase64String(randomNumber);

                _logger.LogInformation("Refresh token generated successfully");

                return refreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating refresh token");
                throw;
            }
        }

        private RefreshToken CreateRefreshToken()
        {

            return new RefreshToken
            {
                Token = GenerateRefreshToken(),
                Expires = DateTime.UtcNow.AddDays(_jwtConfig.RefreshTokenExpirationInDays),
                Created = DateTime.UtcNow
            };
        }

        public bool ValidateAccessToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtConfig.Key);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtConfig.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtConfig.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Token validation failed: {Error}", ex.Message);
                return false;
            }
        }

        public int? GetUserIdFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);

                var userIdClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub);

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error extracting user ID from token: {Error}", ex.Message);
                return null;
            }
        }

        public DateTime GetTokenExpiration(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);

                return jsonToken.ValidTo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error extracting token expiration: {Error}", ex.Message);
                return DateTime.MinValue;
            }
        }
    }
}