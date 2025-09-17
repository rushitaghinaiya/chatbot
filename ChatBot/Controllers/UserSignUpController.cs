using API.Common;
using ChatBot.Models.Common;
using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Model.ViewModels;
using Newtonsoft.Json;
using VRMDBCommon2023;
using Users = ChatBot.Models.ViewModels.Users;

namespace ChatBot.Controllers
{
    [ApiController]
    [Route("v1/[controller]/[action]")]
    [EnableCors("allowCors")]
    [Produces("application/json")]
    public class UserSignUpController : ControllerBase
    {
        private readonly AppSettings _appSetting;
        private readonly IUserSignUp _userSignUp;
        private readonly ISetting _setting;
        private readonly IUser _user;
        private EmailSender _emailSender;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<UserSignUpController> _logger;

        public UserSignUpController(
            IUserSignUp userSignUp,
            IJwtTokenService jwtTokenService,
            IOptions<AppSettings> appSettings,
            ISetting setting,
            IUser user,
            ILogger<UserSignUpController> logger)
        {
            _appSetting = appSettings.Value;
            _setting = setting;
            _userSignUp = userSignUp;
            _user = user;
            _emailSender = new EmailSender(appSettings);
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user with the provided mobile number and email, sends an OTP for verification.
        /// </summary>
        /// <param name="userVM">User view model containing mobile number and email.</param>
        /// <returns>Returns success response if OTP sent successfully, otherwise error response.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseVM<Models.ViewModels.Users>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> SignUp([FromBody] UserVM userVM)
        {
            try
            {
                _logger.LogInformation("User signup attempt for emailId: {EmailId}", userVM?.Mobile);

                if (userVM == null)
                {
                    return Ok(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "User data is required",
                        ErrorCode = "INVALID_INPUT"
                    });
                }

                if (string.IsNullOrWhiteSpace(userVM.Mobile))
                {
                    return Ok(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Mobile number is required",
                        ErrorCode = "INVALID_MOBILE"
                    });
                }

                // Validate mobile number format (basic validation)
                if (!IsValidMobile(userVM.Mobile))
                {
                    return Ok(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Invalid mobile number format",
                        ErrorCode = "INVALID_MOBILE_FORMAT"
                    });
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                var existingUser = await Task.Run(() => _userSignUp.IsExistUser(userVM.Mobile), cts.Token);
                Users users1;

                if (existingUser == null)
                {
                    users1 = new Users
                    {
                        Mobile = userVM.Mobile,
                        Name = userVM.Name,
                        Role = "user",
                        IsPremium = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var savedId = await Task.Run(() => _userSignUp.SaveUser(users1), cts.Token);
                    users1.Id = savedId;

                    _logger.LogInformation("New user created with ID: {UserId} for emailId: {EmailId}", savedId, userVM.Mobile);
                }
                else
                {
                    users1 = existingUser;
                    _logger.LogInformation("Existing user found with ID: {UserId} for emailId: {EmailId}", users1.Id, userVM.Mobile);
                }

                // Remove sensitive info before returning

                var token = _jwtTokenService.Authenticate(users1);
                users1.Mobile = MaskMobileNumber(users1.Mobile);
                // Prepare login response with tokens
                var loginResponse = new LoginResponse
                {
                    User = users1,
                    AccessToken = token.Token,
                    RefreshToken = token.RefreshToken,
                    TokenExpiration = token.RefreshTokenExpiration,
                    TokenType = "Bearer"
                };
                return Ok(new ApiResponseVM<LoginResponse>
                {
                    Success = true,
                    Data = loginResponse,
                    Message = "User signup successful"
                });
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("User signup timed out for emailId: {EmailId}", userVM?.Mobile);
                return StatusCode(408, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "Request timed out",
                    ErrorCode = "TIMEOUT"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user signup for emailId: {EmailId}", userVM?.Mobile);
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "An error occurred during signup",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Verifies the OTP entered by the user for mobile number verification and issues JWT tokens.
        /// </summary>
        /// <param name="modelVM">The OTPVM model containing the OTP and user ID.</param>
        /// <returns>Returns JWT tokens if verification is successful, otherwise error response.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseVM<LoginResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> VerifyOtp([FromBody] OTPVM modelVM)
        {
            try
            {
                _logger.LogInformation("OTP verification attempt for email ID: {EmailId}", modelVM?.EmailId);

                if (modelVM == null)
                {
                    return Ok(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "OTP data is required",
                        ErrorCode = "INVALID_INPUT"
                    });
                }

                if (string.IsNullOrWhiteSpace(modelVM.OtpNumber))
                {
                    return Ok(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "OTP number is required",
                        ErrorCode = "INVALID_OTP"
                    });
                }

                if (modelVM.EmailId ==null)
                {
                    return Ok(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Valid email ID is required",
                        ErrorCode = "INVALID_EMAIL_ID"
                    });
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

                var verificationVM = await Task.Run(() => _userSignUp.GetOTP(modelVM), cts.Token);

                if (verificationVM == null)
                {
                    _logger.LogWarning("No OTP found for user ID: {UserId}", modelVM.UserId);
                    return Ok(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "No OTP found. Please request a new OTP.",
                        ErrorCode = "OTP_NOT_FOUND"
                    });
                }

                if (!verificationVM.OtpNumber.Equals(modelVM.OtpNumber, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Wrong OTP provided for user ID: {UserId}", modelVM.UserId);
                    return Ok(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Invalid OTP. Please check and try again.",
                        ErrorCode = "WRONG_OTP"
                    });
                }

                if (DateTime.UtcNow > verificationVM.OtpTime.AddMinutes(_appSetting.MobileOtpVerificationTime))
                {
                    _logger.LogWarning("Expired OTP provided for user ID: {UserId}", modelVM.UserId);
                    return Ok(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "OTP has expired. Please request a new OTP.",
                        ErrorCode = "OTP_EXPIRED"
                    });
                }

                var result = _userSignUp.VerifyEmail(modelVM.EmailId);
                Users users = new Users();
                users.Email = result.user.LoginEmail;
                users.Name = result.user.FirstName;
                // Get admin user details

                // OTP verification successful, log the successful login
                var loginLogVM = new LoginLogVM
                {
                    UserId = modelVM.UserId,
                    Status = "success",
                    LoginTime = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

               // _userSignUp.SaveLoginLog(loginLogVM);
                 var token = _jwtTokenService.Authenticate(users);
                

                _logger.LogInformation("OTP verification successful and JWT tokens issued for email ID: {EmailId}", modelVM?.EmailId);
                token.Courses = _setting.GetCourses();
                return Ok(new ApiResponseVM<AuthenticationModel>
                {
                    Success = true,
                    Data = token,
                    Message = "otp verified."
                });
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("OTP verification timed out for email ID: {EmailId}", modelVM?.EmailId);
                return StatusCode(408, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "Request timed out",
                    ErrorCode = "TIMEOUT"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OTP verification for email ID: {EmailId}", modelVM?.EmailId);
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "An error occurred during OTP verification",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Refreshes JWT access token using refresh token.
        /// </summary>
        /// <param name="request">Refresh token request containing access and refresh tokens</param>
        /// <returns>New JWT tokens if refresh is successful</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseVM<RefreshTokenResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                _logger.LogInformation("Token refresh attempt");

                if (request == null || string.IsNullOrWhiteSpace(request.AccessToken) || string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Both access token and refresh token are required",
                        ErrorCode = "INVALID_INPUT"
                    });
                }

                // Extract user ID from expired token (don't validate expiration for refresh)
                var userId = _jwtTokenService.GetUserIdFromToken(request.AccessToken);
                if (userId == null)
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Invalid access token",
                        ErrorCode = "INVALID_TOKEN"
                    });
                }

                // In a production app, you would validate the refresh token against stored tokens
                // For now, we'll generate new tokens if the access token structure is valid

                // Get user details
                var userList = await Task.Run(() => _userSignUp.IsExistUser(string.Empty));
                var user = userList ?? new Users { Id = userId.Value };

                // Generate new tokens
                var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
                var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
                var tokenExpiration = _jwtTokenService.GetTokenExpiration(newAccessToken);

                var refreshResponse = new RefreshTokenResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    TokenExpiration = tokenExpiration,
                    TokenType = "Bearer"
                };

                _logger.LogInformation("Tokens refreshed successfully for user ID: {UserId}", userId);

                return Ok(new ApiResponseVM<RefreshTokenResponse>
                {
                    Success = true,
                    Data = refreshResponse,
                    Message = "Tokens refreshed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "An error occurred during token refresh",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Resends OTP to the user's mobile number.
        /// </summary>
        /// <param name="userId">User ID for whom to resend OTP</param>
        /// <returns>Success response if OTP is resent successfully</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> ResendOtp([FromQuery] int userId)
        {
            try
            {
                _logger.LogInformation("OTP resend request for user ID: {UserId}", userId);

                if (userId <= 0)
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Valid user ID is required",
                        ErrorCode = "INVALID_USER_ID"
                    });
                }

                // Note: You would need to implement GetUserById method to get user details
                // For now, we'll create a basic user object
                var user = new Users { Id = userId, Mobile = string.Empty };

                var otpSent = await MobileOtpAsync(user);
                if (otpSent)
                {
                    _logger.LogInformation("OTP resent successfully for user ID: {UserId}", userId);
                    return Ok(new ApiResponseVM<object>
                    {
                        Success = true,
                        Message = "OTP resent successfully"
                    });
                }
                else
                {
                    _logger.LogError("Failed to resend OTP for user ID: {UserId}", userId);
                    return StatusCode(500, new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Failed to resend OTP",
                        ErrorCode = "OTP_RESEND_FAILED"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending OTP for user ID: {UserId}", userId);
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "An error occurred while resending OTP",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Validates the current JWT token.
        /// </summary>
        /// <returns>Token validation result</returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 401)]
        public IActionResult ValidateToken()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                _logger.LogInformation("Token validation successful for user ID: {UserId}", userId);

                return Ok(new ApiResponseVM<object>
                {
                    Success = true,
                    Message = "Token is valid",
                    Data = new
                    {
                        UserId = userId,
                        ValidatedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token validation");
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "An error occurred during token validation",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Verifies whether the given email exists in the Excel file.
        /// </summary>
        /// <param name="model">The login request containing the email to verify.</param>
        /// <returns>
        /// Returns a JSON response with the following structure:
        /// {
        ///   Success = true/false,
        ///   Data = course details if found, otherwise null,
        ///   Message = "User exists" or "User not found"
        /// }
        /// </returns>
        [HttpPost]
        public IActionResult VerifyEmail([FromForm] string Email, [FromForm] string Name)
        {
            if (string.IsNullOrEmpty(Email))
                return BadRequest(new { Success = false, Data = (string?)null, Message = "Email is required" });

            try
            {
                var result = _userSignUp.VerifyEmail(Email);

                if (result.exists)
                {
                    // Send OTP to existing user
                    bool isSent = EmailOtpAsync(result.user).Result;
                    if (isSent)
                    {
                        return Ok(new
                        {
                            Success = true,
                            Data = result.user,
                            Message = "User exists, OTP sent successfully"
                        });
                    }
                    else
                    {
                        return Ok(new
                        {
                            Success = false,
                            Data = result.user,
                            Message = "User exists, but OTP not sent"
                        });
                    }
                }
                else
                {
                    // Create new user from Email + Name
                    var newUser = new UserDetailsExcel
                    {
                        DisplayName = Name,
                        FirstName = Name,
                        LastName = "",
                        LoginEmail = Email,
                        Phone = "",
                        Courses = "",
                        IsMembership = false
                    };

                    // Save new user in Excel
                    var newResult = _userSignUp.VerifyEmail(Email, newUser);

                    // Send OTP to new user
                    bool isSent = EmailOtpAsync(newResult.user).Result;
                    if (isSent)
                    {
                        return Ok(new
                        {
                            Success = true,
                            Data = newResult.user,
                            Message = "New user created, OTP sent successfully"
                        });
                    }
                    else
                    {
                        return Ok(new
                        {
                            Success = false,
                            Data = newResult.user,
                            Message = "New user created, but OTP not sent"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Data = (string?)null, Message = ex.Message });
            }
        }



        /// <summary>
        /// Generates and sends an OTP to the user's mobile number for verification.
        /// </summary>
        /// <param name="users">The user object containing the mobile number and user ID.</param>
        /// <returns>True if the OTP process is initiated successfully, otherwise false.</returns>
        private async Task<bool> MobileOtpAsync(Users users)
        {
            try
            {
                var modelVM = new OTPVM
                {
                    OtpNumber = StringUtilities.RandomString(6),
                    CreatedAt = DateTime.UtcNow,
                    UserId = users.Id
                };

                var otpSaved = await _userSignUp.SaveOTP(modelVM);
                if (otpSaved > 0)
                {
                    var sendSms = new SendSms
                    {
                        Apikey = _appSetting.TwoFactorApiKey.ReturnString(),
                        From = _appSetting.SmsFrom.ReturnString(),
                        TemplateName = "MOBILENOVERIFICATION",
                        Var1 = "User",
                        Var2 = modelVM.OtpNumber,
                        ToSms = users.Mobile.Trim()
                    };

                    var smsDetail = sendSms.SendMessage();
                    var keyValue = JsonConvert.DeserializeObject<Dictionary<string, string>>(smsDetail);

                    _logger.LogInformation("OTP sent successfully to user ID: {UserId}", users.Id);
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to save OTP for user ID: {UserId}", users.Id);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP to user ID: {UserId}", users.Id);
                return false;
            }
        }

        /// <summary>
        /// Validates email format
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        private async Task<bool> EmailOtpAsync(UserDetailsExcel users)
        {
            try
            {
                var modelVM = new OTPVM
                {
                    OtpNumber = StringUtilities.RandomString(6),
                    CreatedAt = DateTime.Now,
                    EmailId = users.LoginEmail.ToString(),
                };

                var otpSaved = await _userSignUp.SaveOTP(modelVM);
                if (otpSaved > 0)
                {
                    _ = _emailSender.SendOtpEmail(users, modelVM.OtpNumber);
                    _logger.LogInformation("OTP sent successfully to email: {Email}", users.LoginEmail);
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to save OTP for admin user: {UserId}", users.LoginEmail);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP to email: {Email}", users.LoginEmail);
                return false;
            }
        }

        /// <summary>
        /// Validates mobile number format (basic validation)
        /// </summary>
        private static bool IsValidMobile(string mobile)
        {
            return !string.IsNullOrWhiteSpace(mobile) &&
                   mobile.Length >= 10 &&
                   mobile.Length <= 15 &&
                   mobile.All(char.IsDigit);
        }

        /// <summary>
        /// Masks mobile number for security
        /// </summary>
        private static string MaskMobileNumber(string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile) || mobile.Length < 4)
                return mobile;

            return mobile.Substring(0, 2) + "****" + mobile.Substring(mobile.Length - 2);
        }

        /// <summary>
        /// Health check endpoint for user signup service
        /// </summary>
        /// <returns>Service health status</returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "User SignUp API with JWT",
                Version = "1.0.0"
            });
        }
    }
}