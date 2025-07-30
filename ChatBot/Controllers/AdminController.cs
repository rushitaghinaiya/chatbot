using Serilog;
using ChatBot.Models.Common;
using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Model.ViewModels;
using Newtonsoft.Json;
using VRMDBCommon2023;

namespace ChatBot.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]/[action]")]
    [EnableCors("allowCors")]
    [Produces("application/json")]
    public class AdminController : ControllerBase
    {
        private readonly AppSettings _appSetting;
        private readonly IUserSignUp _userSignUp;
        private readonly IAdmin _admin;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IUserSignUp userSignUp,
            IAdmin admin,
            IJwtTokenService jwtTokenService,
            IOptions<AppSettings> appSettings,
            ILogger<AdminController> logger)
        {
            _appSetting = appSettings.Value;
            _userSignUp = userSignUp;
            _admin = admin;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates an admin user by mobile number and logs the login event.
        /// This endpoint does not require JWT authentication as it's the login endpoint.
        /// </summary>
        /// <param name="mobile">The mobile number of the admin user.</param>
        /// <returns>Returns success response if login is successful, otherwise an error response.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseVM<Users>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> AdminLogin([FromQuery] string mobile)
        {
            try
            {
                _logger.LogInformation("Admin login attempt for mobile: {Mobile}", mobile);

                if (string.IsNullOrWhiteSpace(mobile))
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Mobile number is required",
                        ErrorCode = "INVALID_INPUT"
                    });
                }

                var users1 = _userSignUp.IsExistUser(mobile);

                if (users1 == null)
                {
                    _logger.LogWarning("Admin login failed - user not found for mobile: {Mobile}", mobile);
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "User not found",
                        ErrorCode = "USER_NOT_FOUND"
                    });
                }

                if (users1.Role != "admin")
                {
                    _logger.LogWarning("Admin login failed - user is not admin for mobile: {Mobile}", mobile);
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "You are not admin",
                        ErrorCode = "INSUFFICIENT_PRIVILEGES"
                    });
                }

                var adminLoginLog = new AdminLoginLog
                {
                    AdminId = users1.Id,
                    LoginTime = DateTime.Now,
                    Actions = "Login"
                };

                _userSignUp.SaveAdminLoginLog(adminLoginLog);

                var otpSent = await MobileOtpAsync(users1);
                if (otpSent)
                {
                    _logger.LogInformation("Admin login successful for mobile: {Mobile}", mobile);
                    return Ok(new ApiResponseVM<Users>
                    {
                        Success = true,
                        Data = users1,
                        Message = "OTP sent successfully"
                    });
                }
                else
                {
                    _logger.LogError("Failed to send OTP for admin login: {Mobile}", mobile);
                    return StatusCode(500, new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Something wrong to send OTP",
                        ErrorCode = "OTP_SEND_FAILED"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin login for mobile: {Mobile}", mobile);
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "An error occurred during admin login",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Verifies admin OTP and issues JWT tokens for admin access.
        /// </summary>
        /// <param name="modelVM">The OTPVM model containing the OTP and user ID.</param>
        /// <returns>Returns JWT tokens if verification is successful, otherwise error response.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseVM<LoginResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> VerifyAdminOtp([FromBody] OTPVM modelVM)
        {
            try
            {
                _logger.LogInformation("Admin OTP verification attempt for user ID: {UserId}", modelVM?.UserId);

                if (modelVM == null)
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "OTP data is required",
                        ErrorCode = "INVALID_INPUT"
                    });
                }

                if (string.IsNullOrWhiteSpace(modelVM.OtpNumber))
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "OTP number is required",
                        ErrorCode = "INVALID_OTP"
                    });
                }

                if (modelVM.UserId <= 0)
                {
                    return BadRequest(new ApiResponseVM<object>
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
                    _logger.LogWarning("No OTP found for admin user ID: {UserId}", modelVM.UserId);
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "No OTP found. Please request a new OTP.",
                        ErrorCode = "OTP_NOT_FOUND"
                    });
                }

                if (!verificationVM.OtpNumber.Equals(modelVM.OtpNumber, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Wrong OTP provided for admin user ID: {UserId}", modelVM.UserId);
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Invalid OTP. Please check and try again.",
                        ErrorCode = "WRONG_OTP"
                    });
                }

                if (DateTime.UtcNow > verificationVM.OtpTime.AddMinutes(_appSetting.MobileOtpVerificationTime))
                {
                    _logger.LogWarning("Expired OTP provided for admin user ID: {UserId}", modelVM.UserId);
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "OTP has expired. Please request a new OTP.",
                        ErrorCode = "OTP_EXPIRED"
                    });
                }

                // Get admin user details
                var adminUser = await Task.Run(() => _userSignUp.IsExistUser(string.Empty), cts.Token);

                // Verify user is admin
                if (adminUser == null || adminUser.Role != "admin")
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Access denied. Admin privileges required.",
                        ErrorCode = "INSUFFICIENT_PRIVILEGES"
                    });
                }

                // Generate JWT tokens for admin
                var accessToken = _jwtTokenService.GenerateAccessToken(adminUser);
                var refreshToken = _jwtTokenService.GenerateRefreshToken();
                var tokenExpiration = _jwtTokenService.GetTokenExpiration(accessToken);

                // Log successful admin login
                var adminLoginLog = new AdminLoginLog
                {
                    AdminId = adminUser.Id,
                    LoginTime = DateTime.UtcNow,
                    Actions = "OTP_VERIFIED"
                };

                _userSignUp.SaveAdminLoginLog(adminLoginLog);

                // Prepare login response with tokens
                var loginResponse = new LoginResponse
                {
                    User = new Users
                    {
                        Id = adminUser.Id,
                        Name = adminUser.Name,
                        Email = adminUser.Email,
                        Mobile = MaskMobileNumber(adminUser.Mobile ?? string.Empty),
                        Role = adminUser.Role,
                        IsPremium = adminUser.IsPremium,
                        CreatedAt = adminUser.CreatedAt,
                        UpdatedAt = DateTime.UtcNow
                    },
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    TokenExpiration = tokenExpiration,
                    TokenType = "Bearer"
                };

                _logger.LogInformation("Admin OTP verification successful and JWT tokens issued for user ID: {UserId}", modelVM.UserId);

                return Ok(new ApiResponseVM<LoginResponse>
                {
                    Success = true,
                    Data = loginResponse,
                    Message = "Admin login successful. Tokens issued."
                });
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Admin OTP verification timed out for user ID: {UserId}", modelVM?.UserId);
                return StatusCode(408, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "Request timed out",
                    ErrorCode = "TIMEOUT"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin OTP verification for user ID: {UserId}", modelVM?.UserId);
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "An error occurred during OTP verification",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Uploads a file and saves its metadata to the database.
        /// Requires JWT authentication and admin role.
        /// </summary>
        /// <param name="file">The file to upload.</param>
        /// <param name="uploadedBy">The ID of the user uploading the file.</param>
        /// <returns>Returns the uploaded file information if successful, otherwise an error response.</returns>
        [HttpPost("upload")]
        [Authorize] // Requires JWT authentication
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 401)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 403)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] int uploadedBy)
        {
            try
            {
                // Get current user from JWT token
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst("role")?.Value;

                _logger.LogInformation("File upload attempt by user: {UserId}, Role: {Role}", currentUserId, userRole);

                // Check if user has admin privileges
                if (userRole != "admin")
                {
                    return StatusCode(403, new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Access denied. Admin privileges required.",
                        ErrorCode = "INSUFFICIENT_PRIVILEGES"
                    });
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "File is empty or not provided",
                        ErrorCode = "INVALID_FILE"
                    });
                }

                if (uploadedBy <= 0)
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Invalid user ID",
                        ErrorCode = "INVALID_USER_ID"
                    });
                }

                // Validate file size (e.g., max 10MB)
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                if (file.Length > maxFileSize)
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "File size exceeds maximum limit of 10MB",
                        ErrorCode = "FILE_TOO_LARGE"
                    });
                }

                var uploadedFile = new UploadFile
                {
                    UploadedBy = uploadedBy,
                    FileName = Path.GetFileName(file.FileName),
                    FileType = file.ContentType,
                    FileSize = (int)file.Length,
                    Status = "Processing",
                    CreatedAt = DateTime.Now,
                    EditedAt = DateTime.Now
                };

                var fileId = _admin.SaveFileMetadataToDatabase(uploadedFile);

                if (fileId > 0)
                {
                    // Log admin action
                    var adminLoginLog = new AdminLoginLog
                    {
                        AdminId = int.Parse(currentUserId ?? "0"),
                        LoginTime = DateTime.UtcNow,
                        Actions = $"FILE_UPLOAD:{uploadedFile.FileName}"
                    };

                    _userSignUp.SaveAdminLoginLog(adminLoginLog);

                    _logger.LogInformation("File uploaded successfully with ID: {FileId} by admin user: {UserId}", fileId, currentUserId);
                    return Ok(new ApiResponseVM<object>
                    {
                        Success = true,
                        Message = "File uploaded successfully",
                        Data = new { FileId = fileId, FileName = uploadedFile.FileName }
                    });
                }
                else
                {
                    _logger.LogError("Failed to save file metadata for user: {UserId}", uploadedBy);
                    return StatusCode(500, new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Failed to save file metadata",
                        ErrorCode = "DATABASE_ERROR"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file upload by user: {UserId}", uploadedBy);
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "An error occurred during file upload",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Gets admin dashboard data. Requires JWT authentication and admin role.
        /// </summary>
        /// <returns>Admin dashboard information</returns>
        [HttpGet]
        [Authorize] // Requires JWT authentication
        [ProducesResponseType(typeof(ApiResponseVM<object>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 401)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 403)]
        public IActionResult GetDashboard()
        {
            try
            {
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst("role")?.Value;
                var userName = User.FindFirst("name")?.Value;

                _logger.LogInformation("Admin dashboard access by user: {UserId}, Role: {Role}", currentUserId, userRole);

                // Check if user has admin privileges
                if (userRole != "admin")
                {
                    return StatusCode(403, new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Access denied. Admin privileges required.",
                        ErrorCode = "INSUFFICIENT_PRIVILEGES"
                    });
                }

                // Return dashboard data
                var dashboardData = new
                {
                    WelcomeMessage = $"Welcome, {userName}",
                    AdminId = currentUserId,
                    LastLogin = DateTime.UtcNow,
                    Stats = new
                    {
                        TotalUsers = 0, // You would implement these based on your business logic
                        ActiveSessions = 0,
                        FilesUploaded = 0,
                        QueriesProcessed = 0
                    }
                };

                return Ok(new ApiResponseVM<object>
                {
                    Success = true,
                    Data = dashboardData,
                    Message = "Dashboard data retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin dashboard");
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving dashboard data",
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
                    CreatedAt = DateTime.Now,
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
                        Var1 = "Admin",
                        Var2 = modelVM.OtpNumber,
                        ToSms = users.Mobile.Trim()
                    };

                    var smsDetail = sendSms.SendMessage();
                    var keyValue = JsonConvert.DeserializeObject<Dictionary<string, string>>(smsDetail);

                    _logger.LogInformation("OTP sent successfully to admin mobile: {Mobile}", users.Mobile);
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to save OTP for admin user: {UserId}", users.Id);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP to admin mobile: {Mobile}", users.Mobile);
                return false;
            }
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
    }
}