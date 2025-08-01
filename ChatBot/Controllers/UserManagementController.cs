using ChatBot.Models.Common;
using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using ChatBot.Repository;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MySqlX.XDevAPI.Common;

namespace ChatBot.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [EnableCors("allowCors")]
    [Produces("application/json")]
    public class UserManagementController : Controller
    {
        private readonly AppSettings _appSetting;
        private readonly IUserSignUp _userSignup;
        private readonly IUserMgmtService _userMgmt;
        private readonly ILogger<QuestionController> _logger;
        public UserManagementController(
          IUserMgmtService userMgmt,
          IUserSignUp userSignup,
          IOptions<AppSettings> appSettings,
          ILogger<QuestionController> logger)
        {
            _appSetting = appSettings.Value;
            _userMgmt = userMgmt;
            _userSignup = userSignup;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves an overview of free users including:
        /// - Total number of free users,
        /// - Number of active free users (who have at least one session),
        /// - Number of inactive free users (who never logged in),
        /// - Number of high-usage free users (who have used 80% or more of their query limit).
        /// </summary>
        /// <returns>
        /// A <see cref="FreeUsersOverviewDto"/> containing aggregated statistics for free users.
        /// </returns>
        [HttpGet("get_free_users_overview")]
        public async Task<ActionResult<FreeUsersOverviewDto>> GetFreeUsersOverview()
        {
            var data = await _userMgmt.GetFreeUsersOverviewAsync();
            return Ok(new ApiResponseVM<FreeUsersOverviewDto>
            {
                Success = true,
                Data = data,
                Message = "Data fetch successfully"
            });
        }

        /// <summary>
        /// Get count of queries grouped by type for free users only.
        /// </summary>
        /// <returns>List of query types and their counts for free users.</returns>
        [HttpGet("get_free_user_query_types")]
        public async Task<ActionResult<List<FreeUserQueryTypeDto>>> GetFreeUserQueryTypes()
        {
            var data = await _userMgmt.GetFreeUserQueryTypesAsync();
            return Ok(new ApiResponseVM<List<FreeUserQueryTypeDto>>
            {
                Success = true,
                Data = data,
                Message = "Data fetch successfully"
            });
        }

        /// <summary>
        /// Gets details of all free users including their communication settings.
        /// </summary>
        /// <returns>
        /// Returns a list of free user details including flags like email, SMS, WhatsApp enable status.
        /// </returns>
        /// <response code="200">Returns the list of free users with their communication preferences.</response>
        [HttpGet("get_free_user_details")]
        public async Task<IActionResult> GetFreeUserDetailsAsync()
        {
            var result = await _userMgmt.GetFreeUserDetailsAsync();
            return Ok(new ApiResponseVM<List<FreeUserDetail>>
            {
                Success = true,
                Data = result,
                Message = "Data fetch successfully"
            });
        }

        /// <summary>
        /// Retrieves all communication settings for all user types.
        /// </summary>
        /// <returns>
        /// Returns a list of communication settings including user type and preferences.
        /// </returns>
        /// <response code="200">Returns the full list of user communication settings.</response>
        [HttpGet("get_communication_settings")]
        public async Task<IActionResult> GetAll()
        {
            var settings = await _userMgmt.GetAllSettingsAsync();
            return Ok(new ApiResponseVM<List<CommunicationSetting>>
            {
                Success = true,
                Data = settings,
                Message = "Data fetch successfully"
            });


        }



        /// <summary>
        /// Updates communication settings (Email, SMS, WhatsApp) for a specific user type.
        /// </summary>
        /// <param name="userType">The user type to update settings for (e.g., 'Free', 'Premium').</param>
        /// <param name="setting">The updated communication settings object.</param>
        /// <returns>Returns success message if updated, or not found if the user type does not exist.</returns>
        /// <response code="200">Returns a success message if update is successful.</response>
        /// <response code="404">Returns an error message if the user type is not found.</response>

        [HttpPut("update_communication_settings")]
        public async Task<IActionResult> Update([FromBody] CommunicationSetting setting)
        {
            setting.UpdatedAt = DateTime.UtcNow;

            var result = await _userMgmt.UpdateSettingAsync(setting);
            if (result > 0)
            {
                var adminLoginLog = new AdminLoginLog
                {
                    AdminId = setting.UpdatedBy,
                    LoginTime = DateTime.Now,
                    Actions = "Communication Settings"
                };

                _userSignup.SaveAdminLoginLog(adminLoginLog);
                return Ok(new { success = true, message = "Updated successfully." });
            }
            else
                return NotFound(new { success = false, message = "UserType not found." });
        }



    }
}
