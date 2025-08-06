using ChatBot.Models.Configuration;
using ChatBot.Models.Responses;
using ChatBot.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ChatBot.Controllers
{

    [Route("v1/[controller]")]
    [ApiController] 
    [Authorize] // Requires JWT authentication
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


        // Gets the list of knowledge bases for a company.
        // Validates input and proxies the request to the Python API.
        // Returns the list of available knowledge bases.
        [HttpGet("kb-list/{companyCode}")]
        [ProducesResponseType(typeof(ApiResponseVM<KnowledgeBaseListResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> GetKnowledgeBaseList(
            [FromRoute] string companyCode,
            [FromQuery] string? dbType = null)
        {
            _logger.LogInformation("Received knowledge base list request for company: {CompanyCode}", companyCode);

            if (string.IsNullOrEmpty(companyCode) || companyCode != _config.CompanyCode)
            {
                return BadRequest(new ApiResponseVM<object>
                {
                    Success = false,
                    Message = $"Invalid company code. Expected: {_config.CompanyCode}",
                });
            }

            var finalDbType = dbType ?? _config.DbType;

            var pythonUrl = $"{_config.PythonApiBaseUrl}/api/v1/medicare-knowledgebase/kb-list/{companyCode}";
            pythonUrl += $"?db_type={Uri.EscapeDataString(finalDbType)}";

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds));

            _logger.LogInformation("Calling Python API: {PythonUrl}", pythonUrl);



            var response = await _httpClient.GetAsync(pythonUrl, cts.Token);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Python API knowledge base list call successful");

                if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "{}")
                {
                    _logger.LogWarning("Python API returned empty response.");
                    return Ok(new ApiResponseVM<KnowledgeBaseListResponse>
                    {
                        Success = false,
                        Message = "Empty response from Python API"
                    });
                }

                var pythonResponse = JsonSerializer.Deserialize<KnowledgeBaseListResponse>(responseContent, _jsonOptions);
                if (pythonResponse == null)
                {
                    _logger.LogWarning("Deserialization returned null.");
                    return Ok(new ApiResponseVM<KnowledgeBaseListResponse>
                    {
                        Success = false,
                        Message = "Invalid response from Python API"
                    });
                }

                // Check if the Python API response indicates success
                if (pythonResponse.StatusCode != 200 || pythonResponse.Status != "success")
                {
                    _logger.LogWarning("Python API returned unsuccessful response. Status: {Status}, StatusCode: {StatusCode}",
                        pythonResponse.Status, pythonResponse.StatusCode);
                    return Ok(new ApiResponseVM<KnowledgeBaseListResponse>
                    {
                        Success = false,
                        Message = $"Python API returned: {pythonResponse.Status}",
                        Data = pythonResponse
                    });
                }

                return Ok(new ApiResponseVM<KnowledgeBaseListResponse>
                {
                    Success = true,
                    Data = pythonResponse,
                    Message = "Success"
                });
            }
            else
            {
                _logger.LogError("Python API knowledge base list call failed. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);

                return BadRequest(new ApiResponseVM<object>
                {
                    Success = false,
                    Message = $"Python API call failed with status {response.StatusCode}",
                });
            }

        }

        // Handles file deletion requests for Medicare knowledge base.
        // Validates input and proxies the request to the Python API.
        // Returns the result of the file deletion operation.
        [HttpDelete("delete-file/{companyCode}")]
        [ProducesResponseType(typeof(ApiResponseVM<DeleteFileResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 400)]
        [ProducesResponseType(typeof(ApiResponseVM<object>), 500)]
        public async Task<IActionResult> DeleteFiles(
            [FromRoute] string companyCode,
            [FromQuery] List<string> files,
            [FromQuery] string? kbName = null,
            [FromQuery] string? dbType = null)
        {
            _logger.LogInformation("Received delete files request for company: {CompanyCode}, Files: {Files}",
                companyCode, string.Join(", ", files));

            if (string.IsNullOrEmpty(companyCode) || companyCode != _config.CompanyCode)
            {
                return BadRequest(new ApiResponseVM<object>
                {
                    Success = false,
                    Message = $"Invalid company code. Expected: {_config.CompanyCode}",
                });
            }

            if (files == null || files.Count == 0)
            {
                return BadRequest(new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "At least one file must be specified for deletion",
                });
            }

            // Validate that all file names are not empty
            if (files.Any(f => string.IsNullOrWhiteSpace(f)))
            {
                return BadRequest(new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "File names cannot be empty",
                });
            }

            var finalKbName = kbName ?? _config.KbName;
            var finalDbType = dbType ?? _config.DbType;

            // Build the URL with query parameters
            var pythonUrl = $"{_config.PythonApiBaseUrl}/api/v1/medicare-knowledgebase/delete-file/{companyCode}";

            var queryParams = new List<string>();

            // Add each file as a separate query parameter
            foreach (var file in files)
            {
                queryParams.Add($"files={Uri.EscapeDataString(file)}");
            }

            queryParams.Add($"kb_name={Uri.EscapeDataString(finalKbName)}");
            queryParams.Add($"db_type={Uri.EscapeDataString(finalDbType)}");

            pythonUrl += "?" + string.Join("&", queryParams);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.TimeoutSeconds));

            _logger.LogInformation("Calling Python API: {PythonUrl}", pythonUrl);

            try
            {
                var response = await _httpClient.DeleteAsync(pythonUrl, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Python API delete files call successful");

                    if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "{}")
                    {
                        _logger.LogWarning("Python API returned empty response.");
                        return Ok(new ApiResponseVM<DeleteFileResponse>
                        {
                            Success = false,
                            Message = "Empty response from Python API"
                        });
                    }

                    var pythonResponse = JsonSerializer.Deserialize<DeleteFileResponse>(responseContent, _jsonOptions);
                    if (pythonResponse == null)
                    {
                        _logger.LogWarning("Deserialization returned null.");
                        return Ok(new ApiResponseVM<DeleteFileResponse>
                        {
                            Success = false,
                            Message = "Invalid response from Python API"
                        });
                    }

                    // Check if the Python API response indicates success
                    if (pythonResponse.StatusCode != 200 || pythonResponse.Status != "success")
                    {
                        _logger.LogWarning("Python API returned unsuccessful response. Status: {Status}, StatusCode: {StatusCode}",
                            pythonResponse.Status, pythonResponse.StatusCode);
                        return Ok(new ApiResponseVM<DeleteFileResponse>
                        {
                            Success = false,
                            Message = $"Python API returned: {pythonResponse.Status}",
                            Data = pythonResponse
                        });
                    }

                    return Ok(new ApiResponseVM<DeleteFileResponse>
                    {
                        Success = true,
                        Data = pythonResponse,
                        Message = "Files deleted successfully"
                    });
                }
                else
                {
                    _logger.LogError("Python API delete files call failed. Status: {StatusCode}, Response: {Response}",
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
                _logger.LogError("Python API delete files call timed out");
                return BadRequest(new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "Request timed out",
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calling Python API for delete files");
                return StatusCode(500, new ApiResponseVM<object>
                {
                    Success = false,
                    Message = "Internal server error",
                });
            }
        }

    }
}