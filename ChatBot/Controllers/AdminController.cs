using ChatBot.Models.Common;
using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Model.ViewModels;
using Newtonsoft.Json;
using System.Reflection;
using VRMDBCommon2023;

namespace ChatBot.Controllers
{
    [ApiController]
    [Route("chatbot/v1/[controller]/[action]")]
    [EnableCors("allowCors")]
    public class AdminController : Controller
    {
        private AppSettings _appSetting;
        private readonly IUserSignUp _userSignUp;
        private readonly IAdmin _admin;
        public AdminController(IUserSignUp userSignUp, IAdmin admin, IOptions<AppSettings> appSettings)
        {
            _appSetting = appSettings.Value;
            _userSignUp = userSignUp;
            _admin = admin;
        }
        /// <summary>
        /// Authenticates an admin user by mobile number and logs the login event.
        /// </summary>
        /// <param name="mobile">The mobile number of the admin user.</param>
        /// <returns>Returns "Success" if login is successful, otherwise a bad request message.</returns>
        [HttpPost]
        public IActionResult AdminLogin(string mobile)
        {
            Users users1 = new Users();
            users1 = _userSignUp.IsExistUser(mobile);
            if (users1.Role == "admin")
            {
                AdminLoginLog adminLoginLog = new AdminLoginLog();
                adminLoginLog.AdminId = users1.Id;
                adminLoginLog.LoginTime = DateTime.Now;
                adminLoginLog.Actions = "Login";
                _userSignUp.SaveAdminLoginLog(adminLoginLog);
                if (MobileOtp(users1))
                {
                    return Ok(new { responseData = users1, status = "Success", isSuccess = true });
                }

                else
                    return Ok(new { responseData = users1, status = "Something wrong to send OTP", isSuccess = false });
            }
            else
                return Ok(new { responseData = users1, status = "You are not admin", isSuccess = false });
        }

        /// <summary>
        /// Uploads a file and saves its metadata to the database.
        /// </summary>
        /// <param name="file">The file to upload.</param>
        /// <param name="uploadedBy">The ID of the user uploading the file.</param>
        /// <returns>Returns the uploaded file's ID, name, and status if successful, otherwise a bad request message.</returns>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] int uploadedBy)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            var uploadedFile = new UploadFile()
            {
                UploadedBy = uploadedBy,
                FileName = Path.GetFileName(file.FileName),
                FileType = file.ContentType,
                FileSize = (int)file.Length,
                Status = "Processing",
                CreatedAt = DateTime.Now,
                EditedAt = DateTime.Now
            };

            int fileId = _admin.SaveFileMetadataToDatabase(uploadedFile);
            if (fileId > 0)
            {
                return Ok(new { status = "File uploaded successfully" });

            }
            return BadRequest(new { status = "File not uploaded" });

        }

        private bool MobileOtp(Users users)
        {
            OTPVM modelVM = new OTPVM();


            modelVM.OtpNumber = StringUtilities.RandomString(6);
            modelVM.CreatedAt = modelVM.CreatedAt = DateTime.Now;
            modelVM.UserId = users.Id;
            if (_userSignUp.SaveOTP(modelVM).Result > 0)
            {

                SendSms sendSms = new SendSms();
                sendSms.Apikey = _appSetting.TwoFactorApiKey.ReturnString();
                sendSms.From = _appSetting.SmsFrom.ReturnString();
                sendSms.TemplateName = "MOBILENOVERIFICATION";
                sendSms.Var1 = "User";// modelVM.UserName;
                sendSms.Var2 = modelVM.OtpNumber;
                sendSms.ToSms = users.Mobile.Trim();
                var smsDetail = sendSms.SendMessage();
                Dictionary<string, string> keyValue = new Dictionary<string, string>();
                keyValue = JsonConvert.DeserializeObject<Dictionary<string, string>>(smsDetail);

            }
            return true;
        }
    }
}
