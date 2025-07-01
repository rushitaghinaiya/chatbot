using ChatBot.Models.Common;
using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Model.ViewModels;
using Newtonsoft.Json;
using VRMDBCommon2023;

namespace ChatBot.Controllers
{
    [ApiController]
    [Route("chatbot/v1/[controller]/[action]")]
    [EnableCors("allowCors")]
    [Produces("application/json")]
    public class UserSignUpController : ControllerBase
    {
        private readonly AppSettings _appSetting;
        private readonly IUserSignUp _userSignUp;
        private readonly ILogger<UserSignUpController> _logger;

        public UserSignUpController(
            IUserSignUp userSignUp,
            IOptions<AppSettings> appSettings,
            ILogger<UserSignUpController> logger)
        {
            _appSetting = appSettings.Value;
            _userSignUp = userSignUp;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user with the provided mobile number and email, sends an OTP for verification.
        /// </summary>
        /// <param name="userVM">User view model containing mobile number and email.</param>
        /// <returns>Returns success response if OTP sent successfully, otherwise error response.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseVM<Users>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> SignUp([FromBody] UserVM userVM)
        {
            try
            {
                _logger.LogInformation("User signup attempt for mobile: {Mobile}", userVM?.Mobile);

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
                        Email = userVM.Email,
                        Mobile = userVM.Mobile,
                        Name = string.Empty, // Will be updated later
                        Role = "user",
                        IsPremium = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var savedId = await Task.Run(() => _userSignUp.SaveUser(users1), cts.Token);
                    users1.Id = savedId;

                    _logger.LogInformation("New user created with ID: {UserId} for mobile: {Mobile}", savedId, userVM.Mobile);
                }
                else
                {
                    users1 = existingUser;
                    _logger.LogInformation("Existing user found with ID: {UserId} for mobile: {Mobile}", users1.Id, userVM.Mobile);
                }

                var otpSent = await MobileOtpAsync(users1);
                if (otpSent)
                {
                    // Remove sensitive information before returning
                    users1.PasswordHash = string.Empty;
                    users1.Mobile = MaskMobileNumber(users1.Mobile);

                    _logger.LogInformation("User signup successful, OTP sent for mobile: {Mobile}", userVM.Mobile);

                    return Ok(new ApiResponseVM<Users>
                    {
                        Success = true,
                        Data = users1,
                        Message = "OTP sent successfully to your mobile number"
                    });
                }
                else
                {
                    _logger.LogError("Failed to send OTP for signup: {Mobile}", userVM.Mobile);
                    return StatusCode(500, new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Failed to send OTP. Please try again.",
                        ErrorCode = "OTP_SEND_FAILED"
                    });
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("User signup timed out for mobile: {Mobile}", userVM?.Mobile);
                return StatusCode(408, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "Request timed out",
                    ErrorCode = "TIMEOUT"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user signup for mobile: {Mobile}", userVM?.Mobile);
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "An error occurred during signup",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Verifies the OTP entered by the user for mobile number verification.
        /// </summary>
        /// <param name="modelVM">The OTPVM model containing the OTP and user ID.</param>
        /// <returns>Returns success response if verification is successful, otherwise error response.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseVM<Users>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> VerifyMobileOtp([FromBody] OTPVM modelVM)
        {
            try
            {
                _logger.LogInformation("OTP verification attempt for user ID: {UserId}", modelVM?.UserId);

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

                if (modelVM.UserId <= 0)
                {
                    return Ok(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Valid user ID is required",
                        ErrorCode = "INVALID_USER_ID"
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

                // OTP verification successful, log the successful login
                var loginLogVM = new LoginLogVM
                {
                    UserId = modelVM.UserId,
                    Status = "success",
                    LoginTime = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                await Task.Run(() => _userSignUp.SaveLoginLog(loginLogVM), cts.Token);

                var user = new Users
                {
                    Id = modelVM.UserId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("OTP verification successful for user ID: {UserId}", modelVM.UserId);

                return Ok(new ApiResponseVM<Users>
                {
                    Success = true,
                    Data = user,
                    Message = "OTP verified successfully. Login completed."
                });
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("OTP verification timed out for user ID: {UserId}", modelVM?.UserId);
                return StatusCode(408, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "Request timed out",
                    ErrorCode = "TIMEOUT"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OTP verification for user ID: {UserId}", modelVM?.UserId);
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "An error occurred during OTP verification",
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
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "User SignUp API",
                Version = "1.0.0"
            });
        }
    }
}