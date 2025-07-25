using ChatBot.Models.Configuration;
using ChatBot.Models.Responses;
using ChatBot.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ChatBot.Controllers
{
    [Route("api/v1/[controller]")]
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

        // Handles file storage requests for Medicare knowledge base.
        // Validates input and proxies the request to the Python API.
        // Returns the result of the file storage operation.
        [HttpPost("store-file/{companyCode}")]
        [Consumes("multipart/form-data")]
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
            IFormFile? file = null)
        {
            _logger.LogInformation("Received store file request for company: {CompanyCode}", companyCode);

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

            var finalKbName = kbName ?? _config.KbName;
            var finalLanguage = language ?? _config.Language;
            var finalDbType = dbType ?? _config.DbType;

            var pythonUrl = $"{_config.PythonApiBaseUrl}/api/v1/medicare-knowledgebase/store-file/{companyCode}";
            if (!string.IsNullOrEmpty(ytUrl))
            {
                pythonUrl += $"?yt_url={Uri.EscapeDataString(ytUrl)}";
            }
            pythonUrl += $"&kb_name={Uri.EscapeDataString(finalKbName)}";
            pythonUrl += $"&language={Uri.EscapeDataString(finalLanguage)}";
            pythonUrl += $"&document_category={Uri.EscapeDataString(documentCategory)}";
            pythonUrl += $"&db_type={Uri.EscapeDataString(finalDbType)}";



            using var formData = new MultipartFormDataContent();
            if (file != null)
            {
                var fileContent = new StreamContent(file.OpenReadStream());
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                formData.Add(fileContent, "file", file.FileName);
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds));

            _logger.LogInformation("Calling Python API: {PythonUrl}", pythonUrl);

            var response = await _httpClient.PostAsync(pythonUrl, formData, cts.Token);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Python API store file call successful");

                var pythonResponse = JsonSerializer.Deserialize<ApiResponseVM<StoreFileResponse>>(responseContent, _jsonOptions);
                return Ok(pythonResponse ?? new ApiResponseVM<StoreFileResponse>
                {
                    Success = false,
                    Message = "Invalid response from Python API",
                });
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

        // Handles Q&A requests for Medicare knowledge base files.
        // Validates input and proxies the question to the Python API.
        // Returns the answer from the knowledge base.

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
            _logger.LogInformation("Received Q&A request for company: {CompanyCode}, Question: {Question}",
                companyCode, question);

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

            var finalKbName = kbName ?? _config.KbName;
            var finalLanguage = language ?? _config.Language;
            var finalDbType = dbType ?? _config.DbType;

            var pythonUrl = $"{_config.PythonApiBaseUrl}/api/v1/medicare-knowledgebase/file-qna/{companyCode}";
            pythonUrl += $"?question={Uri.EscapeDataString(question)}";
            pythonUrl += $"&kb_name={Uri.EscapeDataString(finalKbName)}";
            pythonUrl += $"&language={Uri.EscapeDataString(finalLanguage)}";

            if (!string.IsNullOrEmpty(documentCategory))
            {
                pythonUrl += $"&document_category={Uri.EscapeDataString(documentCategory)}";
            }
            pythonUrl += $"&db_type={Uri.EscapeDataString(finalDbType)}";

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds));

            _logger.LogInformation("Calling Python API: {PythonUrl}", pythonUrl);

            var response = await _httpClient.PostAsync(pythonUrl, null, cts.Token);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Python API Q&A call failed. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);

                return BadRequest(new ApiResponseVM<object>
                {
                    Success = false,
                    Message = $"Python API call failed with status {response.StatusCode}",
                });
            }

            if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "{}")
            {
                _logger.LogWarning("Python API returned empty response.");
                return Ok(new ApiResponseVM<QnAResponse>
                {
                    Success = false,
                    Message = "Empty response from Python API"
                });
            }

            var pythonResponse = JsonSerializer.Deserialize<QnAResponse>(responseContent, _jsonOptions);
            if (pythonResponse == null || string.IsNullOrWhiteSpace(pythonResponse.Answer))
            {
                _logger.LogWarning("Deserialization returned null or missing fields.");
                return Ok(new ApiResponseVM<QnAResponse>
                {
                    Success = false,
                    Message = "Invalid or incomplete response from Python API"
                });
            }

            _logger.LogInformation("Python API Q&A call successful.");
            return Ok(new ApiResponseVM<QnAResponse>
            {
                Success = true,
                Data = pythonResponse,
                Message = "Success"
            });
        }


        // Health check endpoint for the Medicare knowledge base API.
        // Returns service status and configuration details.
        // Used for monitoring and diagnostics.
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