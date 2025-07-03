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
    public class UserSignUpController : Controller
    {
        private AppSettings _appSetting;
        private readonly IUserSignUp _userSignUp;
        public UserSignUpController(IUserSignUp userSignUp, IOptions<AppSettings> appSettings)
        {
            _appSetting = appSettings.Value;
            _userSignUp = userSignUp;
        }
       
       
       
        // Add a logger field to the controller
        private readonly ILogger<UserSignUpController> _logger;

        // Update the constructor to accept ILogger
        public UserSignUpController(IUserSignUp userSignUp, IOptions<AppSettings> appSettings, ILogger<UserSignUpController> logger)
        {
            _appSetting = appSettings.Value;
            _userSignUp = userSignUp;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user with the provided mobile number and sends an OTP for verification.
        /// </summary>
        /// <param name="mobile">The mobile number of the user to sign up.</param>
        /// <returns>Returns Ok if OTP sent successfully, otherwise BadRequest.</returns>
        [HttpPost]
        public IActionResult SignUp(UserVM userVM)
        {
            _logger.LogInformation("SignUp called for mobile: {Mobile}", userVM.Mobile);
            Users users1 = new Users();
            users1 = _userSignUp.IsExistUser(userVM.Mobile);
            if (users1 == null)
            {
                users1 = new Users();
                users1.Email = userVM.Email;
                users1.Mobile = userVM.Mobile;
                users1.CreatedAt = users1.UpdatedAt = DateTime.Now;
                users1.Id = _userSignUp.SaveUser(users1);
                _logger.LogInformation("New user created with ID: {UserId}", users1.Id);
            }
            else
            {
                _logger.LogInformation("User already exists with mobile: {Mobile}", userVM.Mobile);
            }

            if (MobileOtp(users1))
            {
                _logger.LogInformation("OTP sent successfully to mobile: {Mobile}", users1.Mobile);
                return Ok(new { responseData = users1, status = "Success", isSuccess = true });
            }
            else
            {
                _logger.LogWarning("Failed to send OTP to mobile: {Mobile}", users1.Mobile);
                return Ok(new { responseData = users1, status = "Something Went Wrong", isSuccess = false });
            }
        }

        /// <summary>
        /// Verifies the OTP entered by the user for mobile number verification.
        /// </summary>
        /// <param name="modelVM">The OTPVM model containing the OTP and user ID.</param>
        /// <returns>Returns Ok with the result of the verification (Success, OTP expired, Wrong OTP, or No OTP number found).</returns>
        [HttpPost]
        public async Task<IActionResult> VerifyMobileOtp([FromBody] OTPVM modelVM)
        {
            _logger.LogDebug("VerifyMobileOtp called with UserId: {UserId}, OtpNumber: {OtpNumber}", modelVM.UserId, modelVM.OtpNumber);

            Users user = new Users();
            string json = string.Empty;
            OTPVM verificationVM = new OTPVM();

            if (!string.IsNullOrEmpty(modelVM.OtpNumber))
            {
                user.CreatedAt = user.UpdatedAt = DateTime.Now;
                user.Id = modelVM.UserId;
                _logger.LogDebug("Fetching OTP for UserId: {UserId}", modelVM.UserId);
                verificationVM = _userSignUp.GetOTP(modelVM);
                if (verificationVM != null && verificationVM.OtpNumber.Equals(modelVM.OtpNumber, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("OTP matched for UserId: {UserId}", modelVM.UserId);
                    if (DateTime.Now <= verificationVM.OtpTime.AddMinutes(_appSetting.MobileOtpVerificationTime))
                    {
                        _logger.LogDebug("OTP is within valid time window for UserId: {UserId}", modelVM.UserId);
                        LoginLogVM loginLogVM = new LoginLogVM();
                        loginLogVM.UserId = modelVM.UserId;
                        loginLogVM.Status = "success";
                        loginLogVM.LoginTime = DateTime.Now;
                        loginLogVM.CreatedAt = DateTime.Now;
                        _userSignUp.SaveLoginLog(loginLogVM);
                        _logger.LogDebug("Login log saved for UserId: {UserId}", modelVM.UserId);
                        return Ok(new { responseData = user, status = "Success", isSuccess = true });
                    }
                    else
                    {
                        _logger.LogDebug("OTP expired for UserId: {UserId}", modelVM.UserId);
                        return Ok(new { responseData = user, status = "OTP expired.", isSuccess = false });
                    }
                }
                else
                {
                    _logger.LogDebug("Wrong OTP entered for UserId: {UserId}", modelVM.UserId);
                    return Ok(new { responseData = user, status = "Wrong OTP.", isSuccess = false });
                }
            }
            else
            {
                _logger.LogDebug("No OTP number found in request for UserId: {UserId}", modelVM.UserId);
                return Ok(new { responseData = user, status = "No OTP number found.", isSuccess = false });
            }
        }


        /// <summary>
        /// Generates and sends an OTP to the user's mobile number for verification.
        /// </summary>
        /// <param name="users">The user object containing the mobile number and user ID.</param>
        /// <returns>True if the OTP process is initiated (SMS send attempted), otherwise false.</returns>
        private bool MobileOtp(Users users)
        {
            _logger.LogDebug("Starting MobileOtp for UserId: {UserId}, Mobile: {Mobile}", users.Id, users.Mobile);
            OTPVM modelVM = new OTPVM();
            modelVM.OtpNumber = StringUtilities.RandomString(6);
            modelVM.CreatedAt = DateTime.Now;
            modelVM.UserId = users.Id;
            _logger.LogDebug("Generated OTP: {OtpNumber} for UserId: {UserId}", modelVM.OtpNumber, users.Id);
            var saveOtpResult = _userSignUp.SaveOTP(modelVM).Result;
            _logger.LogDebug("SaveOTP result: {SaveOtpResult} for UserId: {UserId}", saveOtpResult, users.Id);
            if (saveOtpResult > 0)
            {
                SendSms sendSms = new SendSms();
                sendSms.Apikey = _appSetting.TwoFactorApiKey.ReturnString();
                sendSms.From = _appSetting.SmsFrom.ReturnString();
                sendSms.TemplateName = "MOBILENOVERIFICATION";
                sendSms.Var1 = "User";
                sendSms.Var2 = modelVM.OtpNumber;
                sendSms.ToSms = users.Mobile.Trim();
                _logger.LogDebug("Sending SMS to: {ToSms} with OTP: {OtpNumber}", sendSms.ToSms, sendSms.Var2);
                var smsDetail = sendSms.SendMessage();
                _logger.LogDebug("SMS send response: {SmsDetail}", smsDetail);
                Dictionary<string, string> keyValue = JsonConvert.DeserializeObject<Dictionary<string, string>>(smsDetail);
                _logger.LogDebug("SMS response deserialized: {@KeyValue}", keyValue);
            }
            else
            {
                _logger.LogWarning("Failed to save OTP for UserId: {UserId}", users.Id);
            }
            return true;
        }
    }
}
