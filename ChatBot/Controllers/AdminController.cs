using ChatBot.Models.Common;
using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace ChatBot.Controllers
{
    [ApiController]
    [Route("chatbot/v1/[controller]/[action]")]
    public class AdminController : Controller
    {
        private AppSettings _appSetting;
        private readonly IUserSignUp _userSignUp;
        private readonly IAdmin _admin;
        public AdminController(IUserSignUp userSignUp,IAdmin admin, IOptions<AppSettings> appSettings)
        {
            _appSetting = appSettings.Value;
            _userSignUp = userSignUp;
            _admin = admin;
        }
        [HttpPost]
        public IActionResult AdminLogin(string mobile)
        {
            Users users1 = new Users();
            users1 = _userSignUp.IsExistUser(mobile);
            if (users1.Role == "admin")
            {
                AdminLoginLog adminLoginLog = new AdminLoginLog();
                adminLoginLog.AdminId = users1.Id;
                adminLoginLog.LoginTime=DateTime.Now;
                adminLoginLog.Actions = "Login";
                _userSignUp.SaveAdminLoginLog(adminLoginLog);
                return Ok("Success");
            }
            else
                return BadRequest("Unable to login");
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile( IFormFile file, [FromForm] int uploadedBy)
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

           
           
                _admin.SaveFileMetadataToDatabase(uploadedFile);
                return Ok(new { uploadedFile.Id, uploadedFile.FileName, uploadedFile.Status });
           
        }
    }
}
