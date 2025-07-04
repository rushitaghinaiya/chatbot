using Serilog;
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
    public class AdminController : ControllerBase
    {
        private readonly AppSettings _appSetting;
        private readonly IUserSignUp _userSignUp;
        private readonly IAdmin _admin;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IUserSignUp userSignUp,
            IAdmin admin,
            IOptions<AppSettings> appSettings,
            ILogger<AdminController> logger)
        {
            _appSetting = appSettings.Value;
            _userSignUp = userSignUp;
            _admin = admin;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates an admin user by mobile number and logs the login event.
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
        /// Uploads a file and saves its metadata to the database.
        /// </summary>
        /// <param name="file">The file to upload.</param>
        /// <param name="uploadedBy">The ID of the user uploading the file.</param>
        /// <returns>Returns the uploaded file information if successful, otherwise an error response.</returns>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] int uploadedBy)
        {
            try
            {
                _logger.LogInformation("File upload attempt by user: {UserId}", uploadedBy);

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
                    _logger.LogInformation("File uploaded successfully with ID: {FileId} by user: {UserId}", fileId, uploadedBy);
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
                        Var1 = "User",
                        Var2 = modelVM.OtpNumber,
                        ToSms = users.Mobile.Trim()
                    };

                    var smsDetail = sendSms.SendMessage();
                    var keyValue = JsonConvert.DeserializeObject<Dictionary<string, string>>(smsDetail);

                    _logger.LogInformation("OTP sent successfully to mobile: {Mobile}", users.Mobile);
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to save OTP for user: {UserId}", users.Id);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP to mobile: {Mobile}", users.Mobile);
                return false;
            }
        }
    }
}