﻿using ChatBot.Models.Common;
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
            var result = await _setting.GetLanguage();
            return Ok(new ApiResponseVM<List<Language>>
            {
                Success = true,
                Data = result,
                Message = "Language fetch successfully"
            });
        }

        [HttpPut("update_language_status")]
        public IActionResult UpdateLanguage(List<Language> model)
        {
            bool updated = false;
            foreach (Language language in model)
            {
                 updated = _setting.UpdateLanguage(language);
            }
            return updated ? Ok(new { message = "Language updated successfully.", status = true }) : BadRequest(new { message = "Update failed.", status = false });
        }

        [HttpGet("get_security_settings")]
        public IActionResult GetSecuritySettings()
        {
            var result = _setting.GetSecuritySettings();
            return Ok(new ApiResponseVM<SecuritySettings>
            {
                Success = true,
                Data = result,
                Message = "Data fetch successfully.",
            });
        }

        [HttpPut("update_security_settings")]
        public IActionResult UpdateSecuritySettings(SecuritySettings model)
        {
            var updated = _setting.UpdateSecuritySettings(model);
            return updated ? Ok(new { message = "Security setting updated successfully." }) : BadRequest("Update failed.");
        }

        // --- Voice Settings ---
        [HttpGet("get_voice_accessibility_settings")]
        public IActionResult GetVoiceSettings()
        {
            var result = _setting.GetVoiceSettings();
            return Ok(new ApiResponseVM<VoiceAccessibilitySettings>
            {
                Success = true,
                Data = result,
                Message = "Data fetch successfully.",
            });
        }

        [HttpPut("update_voice_accessibility_settings")]
        public IActionResult UpdateVoiceSettings(VoiceAccessibilitySettings model)
        {
            var updated = _setting.UpdateVoiceSettings(model);
            return updated ? Ok(new { message = "Voice setting updated successfully.", status = true }) : BadRequest(new { message = "Update failed.", status = false });
        }

        // --- System Limits ---
        [HttpGet("get_system_limits")]
        public IActionResult GetSystemLimits()
        {
            var result = _setting.GetSystemLimits();
            return Ok(new ApiResponseVM<SystemLimits>
            {
                Success = true,
                Data = result,
                Message = "Data fetch successfully.",
            });
        }

        [HttpPut("update_system_limits")]
        public IActionResult UpdateSystemLimits(SystemLimits model)
        {
            var updated = _setting.UpdateSystemLimits(model);
            return updated ? Ok(new { message = "System limit updated successfully.", status = true }) : BadRequest(new { message = "Update failed.", status = false });
        }
    }
}
