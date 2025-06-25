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
        /// <summary>
        /// Registers a new user with the provided mobile number and sends an OTP for verification.
        /// </summary>
        /// <param name="mobile">The mobile number of the user to sign up.</param>
        /// <returns>Returns Ok if OTP sent successfully, otherwise BadRequest.</returns>
        [HttpPost]
        public IActionResult SignUp(UserVM userVM)
        {
            Users users1 = new Users();
            users1 = _userSignUp.IsExistUser(userVM.Mobile);
            if (users1 == null)
            {
                users1 = new Users();
                users1.Email = userVM.Email;
                users1.Mobile = userVM.Mobile;
                users1.CreatedAt = users1.UpdatedAt = DateTime.Now;
                users1.Id = _userSignUp.SaveUser(users1);
            }

            if (MobileOtp(users1))
            {
                return Ok(new { responseData = users1, status = "Success", isSuccess = true });
            }
            else
                return Ok(new { responseData = users1, status = "Something Went Wrong", isSuccess = false });
        }

        /// <summary>
        /// Verifies the OTP entered by the user for mobile number verification.
        /// </summary>
        /// <param name="modelVM">The OTPVM model containing the OTP and user ID.</param>
        /// <returns>Returns Ok with the result of the verification (Success, OTP expired, Wrong OTP, or No OTP number found).</returns>
        [HttpPost]
        public async Task<IActionResult> VerifyMobileOtp([FromBody] OTPVM modelVM)
        {
            Users user = new Users();
            string json = string.Empty;
            OTPVM verificationVM = new OTPVM();

            if (!string.IsNullOrEmpty(modelVM.OtpNumber))
            {
                user.CreatedAt = user.UpdatedAt = DateTime.Now;
                user.Id = modelVM.UserId;
                verificationVM = _userSignUp.GetOTP(modelVM);
                if (verificationVM != null && verificationVM.OtpNumber.Equals(modelVM.OtpNumber, StringComparison.OrdinalIgnoreCase))
                {
                    if (DateTime.Now <= verificationVM.OtpTime.AddMinutes(_appSetting.MobileOtpVerificationTime))
                    {
                        LoginLogVM loginLogVM = new LoginLogVM();
                        loginLogVM.UserId = modelVM.UserId;
                        loginLogVM.Status = "success";
                        loginLogVM.LoginTime = DateTime.Now;
                        loginLogVM.CreatedAt = DateTime.Now;
                        _userSignUp.SaveLoginLog(loginLogVM);
                        return Ok(new { responseData = user, status = "Success", isSuccess = true });
                    }
                    else
                    {
                        return Ok(new { responseData = user, status = "OTP expired.", isSuccess = false });
                    }
                }
                else
                {
                    return Ok(new { responseData = user, status = "Wrong OTP.", isSuccess = false });
                }
            }
            else
            {
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
            OTPVM modelVM = new OTPVM();
            modelVM.OtpNumber = StringUtilities.RandomString(6);
            modelVM.CreatedAt = DateTime.Now;
            modelVM.UserId = users.Id;
            if (_userSignUp.SaveOTP(modelVM).Result > 0)
            {
                SendSms sendSms = new SendSms();
                sendSms.Apikey = _appSetting.TwoFactorApiKey.ReturnString();
                sendSms.From = _appSetting.SmsFrom.ReturnString();
                sendSms.TemplateName = "MOBILENOVERIFICATION";
                sendSms.Var1 = "User";
                sendSms.Var2 = modelVM.OtpNumber;
                sendSms.ToSms = users.Mobile.Trim();
                var smsDetail = sendSms.SendMessage();
                Dictionary<string, string> keyValue = JsonConvert.DeserializeObject<Dictionary<string, string>>(smsDetail);
            }
            return true;
        }
    }
}
