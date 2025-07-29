using ChatBot.Models.Common;
using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ChatBot.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [EnableCors("allowCors")]
    [Produces("application/json")]
    public class SettingController : Controller
    {
        private readonly AppSettings _appSetting;
        private readonly ISetting _setting;
        private readonly ILogger<QuestionController> _logger;

        public SettingController(
            ISetting setting,
            IOptions<AppSettings> appSettings,
            ILogger<QuestionController> logger)
        {
            _appSetting = appSettings.Value;
            _setting = setting;
            _logger = logger;
        }
        [HttpGet("get_languages")]
        public async Task<IActionResult> GetLanguage()
        {
            var result=await _setting.GetLanguage();
            return Ok(new ApiResponseVM<List<Language>>
            {
                Success = true,
                Data = result,
                Message = "Language fetch successfully"
            });
        }

        [HttpGet("security")]
        public IActionResult GetSecuritySettings()
        {
            var result = _setting.GetSecuritySettings();
            return Ok(result);
        }

        [HttpPost("security")]
        public IActionResult UpdateSecuritySettings(SecuritySettings model)
        {
            var updated = _setting.UpdateSecuritySettings(model);
            return updated ? Ok() : BadRequest();
        }

        // --- Voice Settings ---
        [HttpGet("voice")]
        public IActionResult GetVoiceSettings()
        {
            var result = _setting.GetVoiceSettings();
            return Ok(result);
        }

        [HttpPost("voice")]
        public IActionResult UpdateVoiceSettings(VoiceAccessibilitySettings model)
        {
            var updated = _setting.UpdateVoiceSettings(model);
            return updated ? Ok() : BadRequest();
        }

        // --- System Limits ---
        [HttpGet("limits")]
        public IActionResult GetSystemLimits()
        {
            var result = _setting.GetSystemLimits();
            return Ok(result);
        }

        [HttpPost("limits")]
        public IActionResult UpdateSystemLimits(SystemLimits model)
        {
            var updated = _setting.UpdateSystemLimits(model);
            return updated ? Ok() : BadRequest();
        }
    }
}
