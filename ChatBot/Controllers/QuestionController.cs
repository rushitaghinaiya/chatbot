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
    [Authorize] // Requires JWT authentication
    [Route("v1/[controller]/[action]")]
    [EnableCors("allowCors")]
    [Produces("application/json")]
    public class QuestionController : ControllerBase
    {
        private readonly AppSettings _appSetting;
        private readonly IQuestion _question;
        private readonly ILogger<QuestionController> _logger;

        public QuestionController(
            IQuestion question,
            IOptions<AppSettings> appSettings,
            ILogger<QuestionController> logger)
        {
            _appSetting = appSettings.Value;
            _question = question;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all question groups with their associated sub-questions.
        /// </summary>
        /// <returns>A list of question groups with nested sub-questions.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponseVM<List<Question>>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> GetQuestionGroup()
        {
            try
            {
                _logger.LogInformation("Retrieving all question groups");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                var questions = await Task.Run(() => _question.GetQuestionGroup(), cts.Token);

                if (questions == null || !questions.Any())
                {
                    _logger.LogWarning("No question groups found");
                    return Ok(new ApiResponseVM<List<Question>>
                    {
                        Success = true,
                        Data = new List<Question>(),
                        Message = "No question groups found"
                    });
                }

                // Populate sub-questions for each group
                foreach (var questionsItem in questions)
                {
                    try
                    {
                        var questionList = await Task.Run(() => _question.GetQuestionsById(questionsItem.Id), cts.Token);
                        questionsItem.SubQuestion = questionList?.Select(a => a.Text).ToList() ?? new List<string>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load sub-questions for group ID: {GroupId}", questionsItem.Id);
                        questionsItem.SubQuestion = new List<string>();
                    }
                }

                _logger.LogInformation("Successfully retrieved {Count} question groups", questions.Count);

                return Ok(new ApiResponseVM<List<Question>>
                {
                    Success = true,
                    Data = questions,
                    Message = $"Successfully retrieved {questions.Count} question groups"
                });
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Get question groups request timed out");
                return StatusCode(408, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "Request timed out",
                    ErrorCode = "TIMEOUT"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving question groups");
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving question groups",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Retrieves questions by group ID.
        /// </summary>
        /// <param name="id">The group ID to filter questions.</param>
        /// <returns>A list of questions for the specified group ID.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponseVM<List<Question>>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 404)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> GetQuestions([FromQuery] int id)
        {
            try
            {
                _logger.LogInformation("Retrieving questions for group ID: {GroupId}", id);

                if (id <= 0)
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Invalid group ID",
                        ErrorCode = "INVALID_ID"
                    });
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

                var questions = await Task.Run(() => _question.GetQuestionsById(id), cts.Token);

                if (questions == null || !questions.Any())
                {
                    _logger.LogWarning("No questions found for group ID: {GroupId}", id);
                    return NotFound(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "No questions found for the specified group",
                        ErrorCode = "NOT_FOUND"
                    });
                }

                _logger.LogInformation("Successfully retrieved {Count} questions for group ID: {GroupId}",
                    questions.Count, id);

                return Ok(new ApiResponseVM<List<Question>>
                {
                    Success = true,
                    Data = questions,
                    Message = $"Successfully retrieved {questions.Count} questions"
                });
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Get questions request timed out for group ID: {GroupId}", id);
                return StatusCode(408, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "Request timed out",
                    ErrorCode = "TIMEOUT"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving questions for group ID: {GroupId}", id);
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving questions",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Creates a new question in the specified group.
        /// </summary>
        /// <param name="question">The question object to create.</param>
        /// <returns>Success response if question is created successfully.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseVM<Question>), 201)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> CreateQuestion([FromBody] Question question)
        {
            try
            {
                _logger.LogInformation("Creating new question for group ID: {GroupId}", question?.GroupId);

                if (question == null)
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Question data is required",
                        ErrorCode = "INVALID_INPUT"
                    });
                }

                if (string.IsNullOrWhiteSpace(question.Text))
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Question text is required",
                        ErrorCode = "INVALID_INPUT"
                    });
                }

                if (question.GroupId <= 0)
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Valid group ID is required",
                        ErrorCode = "INVALID_GROUP_ID"
                    });
                }

                // Set default values
                question.CreatedAt = DateTime.UtcNow;
                question.UpdatedAt = DateTime.UtcNow;
                question.IsActive = true;

                // Note: You would need to implement CreateQuestion method in IQuestion interface
                // var createdQuestion = await _question.CreateQuestion(question);

                _logger.LogInformation("Question created successfully with ID: {QuestionId}", question.Id);

                return StatusCode(201, new ApiResponseVM<Question>
                {
                    Success = true,
                    Data = question,
                    Message = "Question created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating question for group ID: {GroupId}", question?.GroupId);
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "An error occurred while creating the question",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Health check endpoint for questions service
        /// </summary>
        /// <returns>Service health status</returns>
        [HttpGet]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "Questions API",
                Version = "1.0.0"
            });
        }
    }
}