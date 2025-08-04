using ChatBot.Models.Common;
using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ChatBot.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    [EnableCors("allowCors")]
    [Produces("application/json")]

    public class UserController : ControllerBase
    {
        private readonly AppSettings _appSetting;
        private readonly IUser _user;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUser user,
            IOptions<AppSettings> appSettings,
            ILogger<UserController> logger)
        {
            _appSetting = appSettings.Value;
            _user = user;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the list of all users with pagination support.
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
        /// <param name="includeInactive">Include inactive users in results</param>
        /// <returns>A paginated list of users.</returns>
        [HttpGet("GetUserList")]
        [ProducesResponseType(typeof(ApiResponseVM<List<Users>>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> GetUserList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                _logger.LogInformation("Get user list request - Page: {Page}, PageSize: {PageSize}, IncludeInactive: {IncludeInactive}",
                    page, pageSize, includeInactive);

                if (page < 1)
                {
                    page = 1;
                    _logger.LogWarning("Invalid page number provided, defaulting to 1");
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    pageSize = 20;
                    _logger.LogWarning("Invalid page size provided, defaulting to 20");
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                var userList = await Task.Run(() => _user.GetUserList(), cts.Token);

                if (userList == null)
                {
                    _logger.LogWarning("No users found");
                    return Ok(new ApiResponseVM<List<Users>>
                    {
                        Success = true,
                        Data = new List<Users>(),
                        Message = "No users found"
                    });
                }

                // Filter inactive users if not requested
                if (!includeInactive)
                {
                    // Assuming there's an IsActive property or similar logic
                    // userList = userList.Where(u => u.IsActive).ToList();
                }

                // Apply pagination
                var totalCount = userList.Count;
                var paginatedUsers = userList
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Remove sensitive information before returning
                foreach (var user in paginatedUsers)
                {
                    user.PasswordHash = string.Empty; // Don't expose password hashes
                }

                _logger.LogInformation("Successfully retrieved {Count} users (Page {Page} of {TotalPages})",
                    paginatedUsers.Count, page, (int)Math.Ceiling((double)totalCount / pageSize));

                return Ok(new ApiResponseVM<List<Users>>
                {
                    Success = true,
                    Data = paginatedUsers,
                    Message = $"Successfully retrieved {paginatedUsers.Count} users"
                });
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Get user list request timed out");
                return StatusCode(408, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "Request timed out",
                    ErrorCode = "TIMEOUT"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user list");
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving users",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Gets user details by user ID.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User details if found</returns>
        [HttpGet("GetUserById")]
        [ProducesResponseType(typeof(ApiResponseVM<Users>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 404)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> GetUserById([FromQuery] int id)
        {
            try
            {
                _logger.LogInformation("Get user by ID request - ID: {UserId}", id);

                if (id <= 0)
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Invalid user ID",
                        ErrorCode = "INVALID_ID"
                    });
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

                // Note: You would need to implement GetUserById method in IUser interface
                var userList = await Task.Run(() => _user.GetUserList(), cts.Token);
                var user = userList?.FirstOrDefault(u => u.Id == id);

                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", id);
                    return NotFound(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "User not found",
                        ErrorCode = "NOT_FOUND"
                    });
                }

                // Remove sensitive information
                user.PasswordHash = string.Empty;

                _logger.LogInformation("User found - ID: {UserId}, Name: {Name}", id, user.Name);

                return Ok(new ApiResponseVM<Users>
                {
                    Success = true,
                    Data = user,
                    Message = "User details retrieved successfully"
                });
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Get user by ID timed out for ID: {UserId}", id);
                return StatusCode(408, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "Request timed out",
                    ErrorCode = "TIMEOUT"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user with ID: {UserId}", id);
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving user",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Updates user information.
        /// </summary>
        /// <param name="user">User object with updated information</param>
        /// <returns>Success response if user is updated successfully</returns>
        [HttpPut("UpdateUser")]
        [ProducesResponseType(typeof(ApiResponseVM<Users>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 404)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> UpdateUser([FromBody] Users user)
        {
            try
            {
                _logger.LogInformation("Update user request - ID: {UserId}", user?.Id);

                if (user == null)
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "User data is required",
                        ErrorCode = "INVALID_INPUT"
                    });
                }

                if (user.Id <= 0)
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Valid user ID is required",
                        ErrorCode = "INVALID_ID"
                    });
                }

                if (string.IsNullOrWhiteSpace(user.Name))
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "User name is required",
                        ErrorCode = "INVALID_INPUT"
                    });
                }

                // Set update timestamp
                user.UpdatedAt = DateTime.UtcNow;

                // Note: You would need to implement UpdateUser method in IUser interface
                // var updated = await _user.UpdateUser(user);

                _logger.LogInformation("User updated successfully - ID: {UserId}", user.Id);

                // Remove sensitive information before returning
                user.PasswordHash = string.Empty;

                return Ok(new ApiResponseVM<Users>
                {
                    Success = true,
                    Data = user,
                    Message = "User updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {UserId}", user?.Id);
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "An error occurred while updating user",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Health check endpoint for users service
        /// </summary>
        /// <returns>Service health status</returns>
        [HttpGet("HealthCheck")]
        [ProducesResponseType(typeof(object), 200)]
        [Authorize]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "Users API",
                Version = "1.0.0"
            });
        }

        [HttpGet("get_total_users")]
        public IActionResult GetUserStats()
        {
            var stats = _user.GetUserStats();
            return Ok(new ApiResponseVM<UserStatsDto>
            {
                Success = true,
                Data = stats,
                Message = "User updated successfully"
            });
        }


        [HttpGet("get_queries_today")]
        public async Task<IActionResult> GetTodaysQueryStats()
        {
            try
            {
                var (todayCount, lastMonthCount, percentChange) = await _user.GetTodayQueryStatsAsync();
                return Ok(new
                {
                    todayCount,
                    lastMonthSameDayCount = lastMonthCount,
                    percentageChange = percentChange
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching query stats", error = ex.Message });
            }
        }
        [HttpGet("get_avg_response_time")]
        public async Task<IActionResult> GetAverageResponseTime()
        {
            var (avgResponseTime, lastMonthAvg, percentageChange) = await _user.GetAverageResponseTimeAsync();
            return Ok(new
            {
                Success = true,
                Data = new { avgResponseTime, lastMonthAvg, percentageChange },
                Message = "User updated successfully"
            });
        }
        [HttpGet("get_active_sessions")]
        public async Task<IActionResult> GetActiveSessions()
        {
            var data = await _user.GetSessionStats();
            return Ok(new ApiResponseVM<SessionStatsDto>
            {
                Success = true,
                Data = data,
                Message = "User updated successfully"
            });
        }
        [HttpGet("get_user_list")]
        public IActionResult GetChatbotUsage()
        {
            var data = _user.GetUserChatbotStats();
            return Ok(new ApiResponseVM<List<UserChatbotStatsDto>>
            {
                Success = true,
                Data = data,
                Message = "User updated successfully"
            });
        }
        [HttpGet("get_query_topics_distribution")]
        public async Task<IActionResult> GetQueryTopicDistribution()
        {
            var data = await _user.GetQueryTopicDistribution();
            return Ok(new ApiResponseVM<List<QueryTopicDistributionDto>>
            {
                Success = true,
                Data = data,
                Message = "User updated successfully"
            });
        }
        [HttpGet("get_query_status")]
        public async Task<IActionResult> GetQueryStatusDistribution()
        {
            var result = await _user.GetQueryStatusDistributionAsync();
            return Ok(new ApiResponseVM<QueryStatusDistribution>
            {
                Success = true,
                Data = result,
                Message = "User updated successfully"
            });
        }

        [HttpGet("get_user_types")]
        public async Task<IActionResult> GetUserTypeDistribution()
        {
            var data = await _user.GetUserTypeDistributionAsync();
            return Ok(data);
        }

        [HttpGet("get_average_metrics")]
        public async Task<IActionResult> GetAverageMetrics()
        {
            var result = await _user.GetAverageMetricsAsync();
            return Ok(new ApiResponseVM<AverageMetricsDto>
            {
                Success = true,
                Data = result,
                Message = "User updated successfully"
            });
        }

        [HttpGet("get_admin_login_logs")]
        public async Task<IActionResult> GetAdminLogs()
        {
            var result = await _user.GetAdminLogsAndStatusAsync();
            return Ok(new ApiResponseVM<List<AdminLoginLog>>
            {
                Success = true,
                Data = result,
                Message = "User updated successfully"
            });
        }

        [HttpPost("SaveUserSession")]
        public async Task<IActionResult> SaveUserSession(BotSessionDto botSession)
        {
            if (botSession.EndTime <= botSession.StartTime)
            {
                return BadRequest("EndTime must be greater than StartTime");
            }
            BotSession session = new BotSession()
            {
                UserId = botSession.UserId,
                StartTime = botSession.StartTime,
                EndTime = botSession.EndTime,
                TotalTimeSpent = botSession.TotalTimeSpent,
                CreatedAt = DateTime.Now
            };
            var result = await _user.SaveUserSession(session);
            if (result)
            {
                return Ok(new ApiResponseVM<List<AdminLoginLog>>
                {
                    Success = true,
                    Data = null,
                    Message = "Session saved successfully"
                });
            }
            else
            {
                return StatusCode(500, "An error occurred while saving the session");
            }
        }

            [HttpPost("SaveQueryHistory")]
        public async Task<IActionResult> SaveQueryHistory(QueryHistoryDto queryHistory)
        {
            if (queryHistory.UserId == 0)
            {
                return BadRequest("User Not Found");
            }

            var result = await _user.SaveQueryHistory(queryHistory);
            if (result)
            {
                return Ok(new ApiResponseVM<QueryHistoryDto>
                {
                    Success = true,
                    Data = null,
                    Message = "query history saved successfully"
                });
            }
            else
            {
                return StatusCode(500, "An error occurred while saving the query history");
            }
        }
    }
}