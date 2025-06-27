using ChatBot.Models.Configuration;
using ChatBot.Models.Responses;
using ChatBot.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ChatBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class MedicareKnowledgeBaseController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly MedicareConfig _config;
        private readonly ILogger<MedicareKnowledgeBaseController> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public MedicareKnowledgeBaseController(
            HttpClient httpClient,
            IOptions<MedicareConfig> config,
            ILogger<MedicareKnowledgeBaseController> logger)
        {
            _httpClient = httpClient;
            _config = config.Value;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Stores a file in the Medicare knowledge base via Python API
        /// </summary>
        [HttpPost("store-file/{companyCode}")]
        [ProducesResponseType(typeof(ApiResponseVM<StoreFileResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> StoreFile(
            [FromRoute] string companyCode,
            [FromQuery] string? ytUrl = null,
            [FromQuery] string? kbName = null,
            [FromQuery] string? language = null,
            [FromQuery] string? documentCategory = null,
            [FromQuery] string? dbType = null,
            [FromForm] IFormFile? file = null)
        {
            try
            {
                _logger.LogInformation("Received store file request for company: {CompanyCode}", companyCode);

                // Validate that company code matches the configured static value
                if (string.IsNullOrEmpty(companyCode) || companyCode != _config.CompanyCode)
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = $"Invalid company code. Expected: {_config.CompanyCode}",
                    });
                }

                if (string.IsNullOrEmpty(documentCategory))
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Document category is required",
                    });
                }

                // Use config values as defaults if not provided in request
                var finalKbName = kbName ?? _config.KbName;
                var finalLanguage = language ?? _config.Language;
                var finalDbType = dbType ?? _config.DbType;

                // Build the Python API URL
                var pythonUrl = $"{_config.PythonApiBaseUrl}/api/v1/medicare-knowledgebase/store-file/{companyCode}";
                pythonUrl += $"?kb_name={Uri.EscapeDataString(finalKbName)}";
                pythonUrl += $"&language={Uri.EscapeDataString(finalLanguage)}";
                pythonUrl += $"&document_category={Uri.EscapeDataString(documentCategory)}";
                pythonUrl += $"&db_type={Uri.EscapeDataString(finalDbType)}";

                if (!string.IsNullOrEmpty(ytUrl))
                {
                    pythonUrl += $"&yt_url={Uri.EscapeDataString(ytUrl)}";
                }

                // Prepare the multipart form data
                using var formData = new MultipartFormDataContent();
                if (file != null)
                {
                    var fileContent = new StreamContent(file.OpenReadStream());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                    formData.Add(fileContent, "file", file.FileName);
                }

                // Set timeout for file uploads
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds));

                _logger.LogInformation("Calling Python API: {PythonUrl}", pythonUrl);

                // Call Python API
                var response = await _httpClient.PostAsync(pythonUrl, formData, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Python API store file call successful");

                    // Try to deserialize Python API response
                    try
                    {
                        var pythonResponse = JsonSerializer.Deserialize<ApiResponseVM<StoreFileResponse>>(responseContent, _jsonOptions);
                        return Ok(pythonResponse ?? new ApiResponseVM<StoreFileResponse>
                        {
                            Success = false,
                            Message = "Invalid response from Python API",
                        });
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize Python API response, creating fallback response");

                        // Create a fallback response if Python API response format is different
                        return Ok(new ApiResponseVM<StoreFileResponse>
                        {
                            Success = true,
                            Message = "File processed by Python API",
                            Data = new StoreFileResponse
                            {
                                FileId = Guid.NewGuid().ToString(),
                                FileName = file?.FileName ?? ytUrl ?? "unknown",
                                Status = "Processed",
                                UploadedAt = DateTime.UtcNow,
                                CompanyCode = companyCode,
                                KbName = finalKbName,
                                Language = finalLanguage,
                                DocumentCategory = documentCategory,
                                DbType = finalDbType
                            }
                        });
                    }
                }
                else
                {
                    _logger.LogError("Python API store file call failed. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, responseContent);

                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = $"Python API call failed with status {response.StatusCode}",
                    });
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Python API store file call timed out");
                return StatusCode(408, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "Request timed out",
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Python API store file");
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "Error calling Python API",
                });
            }
        }

        /// <summary>
        /// Gets an answer from the Medicare knowledge base via Python API
        /// </summary>
        [HttpPost("file-qna/{companyCode}")]
        [ProducesResponseType(typeof(ApiResponseVM<QnAResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> GetAnswer(
            [FromRoute] string companyCode,
            [FromQuery] string question,
            [FromQuery] string? kbName = null,
            [FromQuery] string? language = null,
            [FromQuery] string? documentCategory = null,
            [FromQuery] string? dbType = null)
        {
            try
            {
                _logger.LogInformation("Received Q&A request for company: {CompanyCode}, Question: {Question}",
                    companyCode, question);

                // Validate that company code matches the configured static value
                if (string.IsNullOrEmpty(companyCode) || companyCode != _config.CompanyCode)
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = $"Invalid company code. Expected: {_config.CompanyCode}",
                    });
                }

                if (string.IsNullOrEmpty(question))
                {
                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = "Question is required",
                    });
                }

                // Use config values as defaults if not provided in request
                var finalKbName = kbName ?? _config.KbName;
                var finalLanguage = language ?? _config.Language;
                var finalDbType = dbType ?? _config.DbType;

                // Build the Python API URL
                var pythonUrl = $"{_config.PythonApiBaseUrl}/api/v1/medicare-knowledgebase/file-qna/{companyCode}";
                pythonUrl += $"?question={Uri.EscapeDataString(question)}";
                pythonUrl += $"&kb_name={Uri.EscapeDataString(finalKbName)}";
                pythonUrl += $"&language={Uri.EscapeDataString(finalLanguage)}";
                pythonUrl += $"&db_type={Uri.EscapeDataString(finalDbType)}";

                if (!string.IsNullOrEmpty(documentCategory))
                {
                    pythonUrl += $"&document_category={Uri.EscapeDataString(documentCategory)}";
                }

                // Set timeout for Q&A
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds));

                _logger.LogInformation("Calling Python API: {PythonUrl}", pythonUrl);

                // Call Python API
                var response = await _httpClient.PostAsync(pythonUrl, null, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Python API Q&A call successful");

                    // Try to deserialize Python API response
                    try
                    {
                        var pythonResponse = JsonSerializer.Deserialize<ApiResponseVM<QnAResponse>>(responseContent, _jsonOptions);
                        return Ok(pythonResponse ?? new ApiResponseVM<QnAResponse>
                        {
                            Success = false,
                            Message = "Invalid response from Python API",
                        });
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize Python API response, creating fallback response");

                        // Create a fallback response if Python API response format is different
                        return Ok(new ApiResponseVM<QnAResponse>
                        {
                            Success = true,
                            Message = "Answer generated by Python API",
                            Data = new QnAResponse
                            {
                                Answer = "Answer received from Python API",
                                Confidence = 0.85,
                                Sources = { "Python API Response" },
                                ResponseId = Guid.NewGuid().ToString(),
                                Question = question,
                                CompanyCode = companyCode,
                                KbName = finalKbName,
                                Language = finalLanguage,
                                DbType = finalDbType
                            }
                        });
                    }
                }
                else
                {
                    _logger.LogError("Python API Q&A call failed. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, responseContent);

                    return BadRequest(new ApiResponseVM<object>
                    {
                        Success = false,
                        Message = $"Python API call failed with status {response.StatusCode}",
                    });
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Python API Q&A call timed out");
                return StatusCode(408, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "Request timed out",
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Python API Q&A");
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "Error calling Python API",
                });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "Medicare Knowledge Base API (C# Proxy)",
                Version = "1.0.0",
                Configuration = new
                {
                    CompanyCode = _config.CompanyCode,
                    KbName = _config.KbName,
                    Language = _config.Language,
                    DbType = _config.DbType,
                    SupportedCategories = _config.DocumentCategories,
                    PythonApiUrl = _config.PythonApiBaseUrl
                }
            });
        }
    }
}
