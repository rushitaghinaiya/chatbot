using ChatBot.Models.Services;
using Microsoft.AspNetCore.Mvc;
using ChatBot.Models.Entities;
using ChatBot.Models.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace ChatBot.Controllers
{
    [ApiController]
    [Route("api/v1/knowledgebase")]
    [Produces("application/json")]
    public class KnowledgeBaseController : ControllerBase
    {
        private readonly IKnowledgeBase _knowledgeBase;
        private readonly ILogger<KnowledgeBaseController> _logger;

        public KnowledgeBaseController(
            IKnowledgeBase knowledgeBase,
            ILogger<KnowledgeBaseController> logger)
        {
            _knowledgeBase = knowledgeBase;
            _logger = logger;
        }

        /// <summary>
        /// Process a question against the knowledge base files
        /// </summary>
        /// <param name="companycode">Company identifier</param>
        /// <param name="question">Question to ask</param>
        /// <param name="kb_name">Knowledge base name</param>
        /// <param name="language">Language for processing</param>
        /// <param name="document_category">Optional document category filter</param>
        /// <param name="db_type">Database type</param>
        /// <returns>Answer from knowledge base</returns>
        [HttpGet("file-qna/{companycode}")]
        [ProducesResponseType(typeof(ApiResponseVM<FileQnAVM>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 404)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> FileQnA(
            [FromRoute][Required] string companycode,
            [FromQuery][Required] string question,
            [FromQuery][Required] string kb_name,
            [FromQuery][Required] string language,
            [FromQuery] string? document_category = null,
            [FromQuery][Required] string db_type = "")
        {
            try
            {
                _logger.LogInformation("Processing QnA request for company: {CompanyCode}, KB: {KbName}",
                    companycode, kb_name);

                // Validate input parameters
                if (string.IsNullOrWhiteSpace(question))
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Question parameter is required",
                        ErrorCode = "INVALID_QUESTION"
                    });
                }

                if (string.IsNullOrWhiteSpace(kb_name))
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Knowledge base name is required",
                        ErrorCode = "INVALID_KB_NAME"
                    });
                }

                if (string.IsNullOrWhiteSpace(language))
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Language parameter is required",
                        ErrorCode = "INVALID_LANGUAGE"
                    });
                }

                if (string.IsNullOrWhiteSpace(db_type))
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Database type is required",
                        ErrorCode = "INVALID_DB_TYPE"
                    });
                }

                // Validate company code
                var isValidCompany = await _knowledgeBase.ValidateCompanyCodeAsync(companycode);
                if (!isValidCompany)
                {
                    return NotFound(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Company not found",
                        ErrorCode = "COMPANY_NOT_FOUND"
                    });
                }

                // Validate knowledge base
                var isValidKb = await _knowledgeBase.ValidateKnowledgeBaseAsync(companycode, kb_name);
                if (!isValidKb)
                {
                    return NotFound(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Knowledge base not found",
                        ErrorCode = "KB_NOT_FOUND"
                    });
                }

                var request = new FileQnARequest
                {
                    Question = question,
                    KbName = kb_name,
                    Language = language,
                    DocumentCategory = document_category,
                    DbType = db_type
                };

                var result = await _knowledgeBase.ProcessQuestionAsync(companycode, request);

                return Ok(new ApiResponseVM<FileQnAVM>
                {
                    Success = result.Success,
                    Data = result,
                    Message = result.Success ? "Question processed successfully" : "Failed to process question"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument in QnA request");
                return BadRequest(new ApiResponseVM<object>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "INVALID_ARGUMENT"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing QnA request for company: {CompanyCode}", companycode);
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "Internal server error",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Store files in the knowledge base
        /// </summary>
        /// <param name="companycode">Company identifier</param>
        /// <param name="yt_url">Optional YouTube URL</param>
        /// <param name="kb_name">Knowledge base name</param>
        /// <param name="language">Language for processing</param>
        /// <param name="document_category">Optional document category</param>
        /// <param name="db_type">Database type</param>
        /// <returns>Upload results</returns>
        [HttpPost("store-file/{companycode}")]
        [ProducesResponseType(typeof(ApiResponseVM<StoreFileVM>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 404)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 413)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        [RequestSizeLimit(100_000_000)] // 100MB limit
        public async Task<IActionResult> StoreFile(
            [FromRoute][Required] string companycode,
            [FromQuery] string? yt_url = null,
            [FromQuery][Required] string kb_name = "",
            [FromQuery][Required] string language = "",
            [FromQuery] string? document_category = null,
            [FromQuery][Required] string db_type = "")
        {
            try
            {
                _logger.LogInformation("Processing file storage request for company: {CompanyCode}, KB: {KbName}",
                    companycode, kb_name);

                // Validate required parameters
                if (string.IsNullOrWhiteSpace(kb_name))
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Knowledge base name is required",
                        ErrorCode = "INVALID_KB_NAME"
                    });
                }

                if (string.IsNullOrWhiteSpace(language))
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Language parameter is required",
                        ErrorCode = "INVALID_LANGUAGE"
                    });
                }

                if (string.IsNullOrWhiteSpace(db_type))
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Database type is required",
                        ErrorCode = "INVALID_DB_TYPE"
                    });
                }

                // Validate that either files or YouTube URL is provided
                var hasFiles = Request.Form.Files.Count > 0;
                var hasYouTubeUrl = !string.IsNullOrWhiteSpace(yt_url);

                if (!hasFiles && !hasYouTubeUrl)
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Either files or YouTube URL must be provided",
                        ErrorCode = "NO_CONTENT_PROVIDED"
                    });
                }

                // Validate YouTube URL format if provided
                if (hasYouTubeUrl && !IsValidYouTubeUrl(yt_url!))
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Invalid YouTube URL format",
                        ErrorCode = "INVALID_YOUTUBE_URL"
                    });
                }

                // Validate file types if files are provided
                if (hasFiles)
                {
                    var invalidFiles = Request.Form.Files
                        .Where(f => !IsValidFileType(f))
                        .Select(f => f.FileName)
                        .ToList();

                    if (invalidFiles.Any())
                    {
                        return BadRequest(new ApiResponseVM<object>
                        {
                            Success = false,
                            Message = $"Invalid file types: {string.Join(", ", invalidFiles)}. Only PDF files are allowed.",
                            ErrorCode = "INVALID_FILE_TYPE"
                        });
                    }
                }

                // Validate company code
                var isValidCompany = await _knowledgeBase.ValidateCompanyCodeAsync(companycode);
                if (!isValidCompany)
                {
                    return NotFound(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Company not found",
                        ErrorCode = "COMPANY_NOT_FOUND"
                    });
                }

                var request = new StoreFileRequest
                {
                    YtUrl = yt_url,
                    KbName = kb_name,
                    Language = language,
                    DocumentCategory = document_category,
                    DbType = db_type,
                    Files = Request.Form.Files.ToList()
                };

                var result = await _knowledgeBase.StoreFilesAsync(companycode, request);

                return Ok(new ApiResponseVM<StoreFileVM>
                {
                    Success = result.Success,
                    Data = result,
                    Message = result.Success ? "Files stored successfully" : "Failed to store files"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument in store file request");
                return BadRequest(new ApiResponseVM<object>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "INVALID_ARGUMENT"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing files for company: {CompanyCode}", companycode);
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "Internal server error",
                    ErrorCode = "INTERNAL_ERROR"
                });
            }
        }

        private static bool IsValidFileType(IFormFile file)
        {
            var allowedExtensions = new[] { ".pdf" };
            var allowedMimeTypes = new[] { "application/pdf" };

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension) &&
                   allowedMimeTypes.Contains(file.ContentType);
        }

        private static bool IsValidYouTubeUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                return (uri.Host.Contains("youtube.com") || uri.Host.Contains("youtu.be")) &&
                       !string.IsNullOrWhiteSpace(uri.Query);
            }
            catch
            {
                return false;
            }
        }
    }
}
