using Serilog;
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
            Log.Information("AdminLogin endpoint called at {Time} by {mobile}", DateTime.Now, mobile);
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
            Log.Information("UploadFile endpoint called at {Time} by {UploadedBy}", DateTime.Now, uploadedBy);

            if (file == null || file.Length == 0)
            {
                Log.Warning("File is empty. UploadedBy: {UploadedBy}", uploadedBy);
                return BadRequest("File is empty");
            }

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

            Log.Debug("Prepared UploadFile metadata: {@UploadFile}", uploadedFile);

            int fileId = _admin.SaveFileMetadataToDatabase(uploadedFile);
            Log.Debug("SaveFileMetadataToDatabase returned fileId: {FileId}", fileId);

            if (fileId > 0)
            {
                Log.Information("File uploaded successfully. FileId: {FileId}, UploadedBy: {UploadedBy}", fileId, uploadedBy);
                return Ok(new { status = "File uploaded successfully" });
            }
            else
            {
                Log.Warning("File not uploaded. UploadedBy: {UploadedBy}", uploadedBy);
                return BadRequest(new { status = "File not uploaded" });
            }
        }

        private bool MobileOtp(Users users)
        {
            Log.Information("MobileOtp called for UserId: {UserId}, Mobile: {Mobile}", users.Id, users.Mobile);

            OTPVM modelVM = new OTPVM();
            modelVM.OtpNumber = StringUtilities.RandomString(6);
            modelVM.CreatedAt = DateTime.Now;
            modelVM.UserId = users.Id;

            Log.Debug("Generated OTP: {OtpNumber} for UserId: {UserId}", modelVM.OtpNumber, users.Id);

            var saveOtpResult = _userSignUp.SaveOTP(modelVM).Result;
            Log.Debug("SaveOTP result: {SaveOtpResult} for UserId: {UserId}", saveOtpResult, users.Id);

            if (saveOtpResult > 0)
            {
                SendSms sendSms = new SendSms
                {
                    Apikey = _appSetting.TwoFactorApiKey.ReturnString(),
                    From = _appSetting.SmsFrom.ReturnString(),
                    TemplateName = "MOBILENOVERIFICATION",
                    Var1 = "User",
                    Var2 = modelVM.OtpNumber,
                    ToSms = users.Mobile?.Trim()
                };

                Log.Information("Sending OTP SMS to: {ToSms} for UserId: {UserId}", sendSms.ToSms, users.Id);

                var smsDetail = sendSms.SendMessage();
                Log.Debug("SMS send response: {SmsDetail} for UserId: {UserId}", smsDetail, users.Id);

                Dictionary<string, string> keyValue = JsonConvert.DeserializeObject<Dictionary<string, string>>(smsDetail);
                Log.Debug("Deserialized SMS response: {@KeyValue} for UserId: {UserId}", keyValue, users.Id);
            }
            else
            {
                Log.Warning("Failed to save OTP for UserId: {UserId}", users.Id);
            }

            return true;
        }
    }
}
