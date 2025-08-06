using ChatBot.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;

namespace ChatBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires JWT authentication
    public class MedlinePlusController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MedlinePlusController> _logger;
        private readonly IConfiguration _configuration;
        private const string BaseUrl = "https://wsearch.nlm.nih.gov/ws/query";

        public MedlinePlusController(HttpClient httpClient, ILogger<MedlinePlusController> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet("health-topics")]
        public async Task<ActionResult<MedlinePlusResponse>> SearchHealthTopics(
            [FromQuery] string query,
            [FromQuery] int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("Query parameter is required for health topics search");
                return BadRequest("Query parameter is required");
            }

            // Validate if query is medical-related
            if (!IsMedicalQuery(query))
            {
                return Ok(GetNonMedicalResponse(query));
            }

            try
            {
                var url = $"{BaseUrl}?db=healthTopics&term={Uri.EscapeDataString(query)}&retmax={maxResults}&rettype=brief";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = ParseMedlinePlusResponse(content);

                // Add source information
                result.DataSource = "MedlinePlus";

                _logger.LogInformation("Successfully retrieved {Count} health topics for query: {Query}",
                    result.Documents?.Count ?? 0, query);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MedlinePlus API failed for health topics search: {Query}. Falling back to OpenAI", query);

                var fallbackResult = await GetOpenAiFallbackResponse(query, "health topics", maxResults);
                return Ok(fallbackResult);
            }
        }

        [HttpGet("drugs")]
        public async Task<ActionResult<MedlinePlusResponse>> SearchDrugs(
            [FromQuery] string drugName,
            [FromQuery] int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(drugName))
            {
                _logger.LogWarning("Drug name parameter is required for drug search");
                return BadRequest("Drug name parameter is required");
            }

            // Validate if query is medical-related
            if (!IsMedicalQuery(drugName))
            {
                return Ok(GetNonMedicalResponse(drugName));
            }

            try
            {
                var url = $"{BaseUrl}?db=drugs&term={Uri.EscapeDataString(drugName)}&retmax={maxResults}&rettype=brief";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = ParseMedlinePlusResponse(content);

                // Add source information
                result.DataSource = "MedlinePlus";

                _logger.LogInformation("Successfully retrieved {Count} drug entries for: {DrugName}",
                    result.Documents?.Count ?? 0, drugName);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MedlinePlus API failed for drug search: {DrugName}. Falling back to OpenAI", drugName);

                var fallbackResult = await GetOpenAiFallbackResponse(drugName, "drugs and medications", maxResults);
                return Ok(fallbackResult);
            }
        }

        [HttpGet("encyclopedia")]
        public async Task<ActionResult<MedlinePlusResponse>> SearchEncyclopedia(
            [FromQuery] string topic,
            [FromQuery] int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                _logger.LogWarning("Topic parameter is required for encyclopedia search");
                return BadRequest("Topic parameter is required");
            }

            // Validate if query is medical-related
            if (!IsMedicalQuery(topic))
            {
                return Ok(GetNonMedicalResponse(topic));
            }

            try
            {
                var url = $"{BaseUrl}?db=encyclopedia&term={Uri.EscapeDataString(topic)}&retmax={maxResults}&rettype=brief";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = ParseMedlinePlusResponse(content);

                // Add source information
                result.DataSource = "MedlinePlus";

                _logger.LogInformation("Successfully retrieved {Count} encyclopedia entries for: {Topic}",
                    result.Documents?.Count ?? 0, topic);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MedlinePlus API failed for encyclopedia search: {Topic}. Falling back to OpenAI", topic);

                var fallbackResult = await GetOpenAiFallbackResponse(topic, "medical encyclopedia", maxResults);
                return Ok(fallbackResult);
            }
        }

        [HttpGet("document/{documentId}")]
        public async Task<ActionResult<MedlinePlusDocument>> GetDocument(string documentId)
        {
            if (string.IsNullOrWhiteSpace(documentId))
            {
                _logger.LogWarning("Document ID is required for document retrieval");
                return BadRequest("Document ID is required");
            }

            try
            {
                var url = $"{BaseUrl}?db=all&term={Uri.EscapeDataString(documentId)}&rettype=full";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = ParseMedlinePlusResponse(content);

                var document = result.Documents?.FirstOrDefault();
                if (document == null)
                {
                    _logger.LogWarning("Document with ID {DocumentId} not found", documentId);
                    return NotFound($"Document with ID {documentId} not found");
                }

                _logger.LogInformation("Successfully retrieved document: {DocumentId}", documentId);

                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MedlinePlus API failed for document retrieval: {DocumentId}. Falling back to OpenAI", documentId);

                var fallbackResult = await GetOpenAiFallbackResponse(documentId, "medical information", 1);
                var document = fallbackResult.Documents?.FirstOrDefault();

                if (document == null)
                {
                    return NotFound($"Document with ID {documentId} not found");
                }

                return Ok(document);
            }
        }

        [HttpGet("search-all")]
        public async Task<ActionResult<MedlinePlusResponse>> SearchAll(
            [FromQuery] string query,
            [FromQuery] int maxResults = 20)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("Query parameter is required for search-all");
                return BadRequest("Query parameter is required");
            }

            // Validate if query is medical-related
            if (!IsMedicalQuery(query))
            {
                return Ok(GetNonMedicalResponse(query));
            }

            try
            {
                var url = $"{BaseUrl}?db=all&term={Uri.EscapeDataString(query)}&retmax={maxResults}&rettype=brief";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = ParseMedlinePlusResponse(content);

                // Add source information
                result.DataSource = "MedlinePlus";

                _logger.LogInformation("Successfully retrieved {Count} results across all databases for: {Query}",
                    result.Documents?.Count ?? 0, query);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MedlinePlus API failed for search-all: {Query}. Falling back to OpenAI", query);

                var fallbackResult = await GetOpenAiFallbackResponse(query, "comprehensive medical information", maxResults);
                return Ok(fallbackResult);
            }
        }

        [HttpGet("health-check")]
        public async Task<ActionResult<object>> HealthCheck()
        {
            try
            {
                var url = $"{BaseUrl}?db=healthTopics&term=diabetes&retmax=1";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("MedlinePlus API health check successful");

                return Ok(new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    ApiEndpoint = BaseUrl,
                    Source = "MedlinePlus"
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MedlinePlus API health check failed. Testing OpenAI fallback");

                try
                {
                    // Test OpenAI connection
                    var testResult = await GetOpenAiFallbackResponse("diabetes", "health check", 1);

                    return Ok(new
                    {
                        Status = "Healthy (Fallback)",
                        Timestamp = DateTime.UtcNow,
                        ApiEndpoint = "OpenAI",
                        Source = "OpenAI",
                        Warning = "MedlinePlus API unavailable"
                    });
                }
                catch (Exception openAiEx)
                {
                    _logger.LogError(openAiEx, "Both MedlinePlus and OpenAI APIs failed during health check");

                    return StatusCode(503, new
                    {
                        Status = "Unhealthy",
                        Timestamp = DateTime.UtcNow,
                        Source = "None",
                        Error = "Both primary and fallback APIs are unavailable"
                    });
                }
            }
        }

        /// <summary>
        /// Validates if a query is medical-related
        /// </summary>
        private bool IsMedicalQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return false;

            var medicalKeywords = new[]
            {
                // Symptoms and conditions
                "pain", "fever", "headache", "nausea", "vomiting", "diarrhea", "constipation", "fatigue", "weakness",
                "dizziness", "shortness of breath", "chest pain", "abdominal pain", "back pain", "joint pain",
                "muscle pain", "sore throat", "cough", "cold", "flu", "infection", "inflammation", "allergy",
                "asthma", "diabetes", "hypertension", "depression", "anxiety", "insomnia", "cancer", "tumor",
                "heart disease", "stroke", "arthritis", "migraine", "epilepsy", "pneumonia", "bronchitis",
                
                // Medical terms
                "diagnosis", "treatment", "therapy", "medication", "prescription", "dosage", "side effects",
                "symptoms", "disease", "disorder", "syndrome", "condition", "illness", "injury", "wound",
                "surgery", "operation", "procedure", "test", "screening", "examination", "blood pressure",
                "blood sugar", "cholesterol", "vaccine", "vaccination", "immunization", "antibiotics",
                "virus", "bacteria", "parasite", "fungal", "genetic", "hereditary", "chronic", "acute",
                
                // Body parts and systems
                "heart", "lung", "liver", "kidney", "brain", "stomach", "intestine", "skin", "bone", "muscle",
                "blood", "nerve", "hormone", "immune", "respiratory", "cardiovascular", "digestive", "nervous",
                "endocrine", "reproductive", "urinary", "skeletal", "muscular", "lymphatic",
                
                // Common medications
                "aspirin", "ibuprofen", "acetaminophen", "paracetamol", "insulin", "antibiotics", "antihistamine",
                "antidepressant", "painkiller", "vitamin", "supplement", "medicine", "drug", "tablet", "capsule",
                "injection", "ointment", "cream", "drops",
                
                // Medical questions
                "health", "medical", "doctor", "physician", "nurse", "hospital", "clinic", "emergency",
                "pregnant", "pregnancy", "child", "pediatric", "elderly", "senior", "safe", "dangerous",
                "normal", "abnormal", "healthy", "unhealthy", "risk", "prevent", "prevention", "cure",
                "healing", "recovery", "rehabilitation"
            };

            var queryLower = query.ToLower();
            return medicalKeywords.Any(keyword => queryLower.Contains(keyword));
        }

        /// <summary>
        /// Returns a response for non-medical queries
        /// </summary>
        private MedlinePlusResponse GetNonMedicalResponse(string query)
        {
            return new MedlinePlusResponse
            {
                Documents = new List<MedlinePlusDocument>
                {
                    new MedlinePlusDocument
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = "Medical Information Assistant",
                        Summary = "I specialize in health topics.",
                        Content = "Ask me anything related to medical information, such as conditions, symptoms, or medications.",
                        DataSource = "System",
                        Url = "",
                        Rank = 1,
                        LastUpdated = DateTime.UtcNow,
                        Keywords = new List<string> { "medical", "health", "assistant" },
                        AltTitles = new List<string>(),
                        GroupNames = new List<string> { "System" },
                        MeshTerms = new List<string>()
                    }
                },
                TotalResults = 1,
                Term = query,
                RetStart = 0,
                RetMax = 1,
                Timestamp = DateTime.UtcNow,
                DataSource = "System"
            };
        }

        /// <summary>
        /// Gets a fallback response from OpenAI when MedlinePlus API is unavailable.
        /// Formats the response to match MedlinePlus structure.
        /// </summary>
        private async Task<MedlinePlusResponse> GetOpenAiFallbackResponse(string query, string category, int maxResults)
        {
            try
            {
                _logger.LogInformation("Requesting OpenAI fallback for query: {Query} in category: {Category}", query, category);

                var openAiApiKey = _configuration["OpenAI:ApiKey"];
                if (string.IsNullOrEmpty(openAiApiKey))
                {
                    throw new InvalidOperationException("OpenAI API key not configured");
                }

                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "You are a medical information assistant. Provide accurate, evidence-based medical information. Always recommend consulting healthcare professionals for medical advice."
                        },
                        new
                        {
                            role = "user",
                            content = $@"Provide medical information about '{query}' related to {category}. 
                            Structure your response as multiple medical documents, each with:
                            - A clear title
                            - A brief summary (2-3 sentences)
                            - Detailed content explaining the topic
                            - Relevant medical source organization
                            
                            Provide up to {Math.Min(maxResults, 5)} different aspects about this topic.
                            Format each document clearly separated with '---DOCUMENT---' as a separator.
                            
                            Only provide accurate, evidence-based medical information."
                        }
                    },
                    max_tokens = 2000,
                    temperature = 0.3
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAiApiKey}");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                dynamic responseObj = JsonConvert.DeserializeObject(responseContent);
                string openAiResponse = responseObj.choices[0].message.content;

                var result = FormatOpenAiResponseAsMedlinePlus(openAiResponse, query, maxResults);

                _logger.LogInformation("Successfully generated OpenAI fallback response with {Count} documents for query: {Query}",
                    result.Documents?.Count ?? 0, query);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI fallback failed for query: {Query}", query);

                // Return a minimal response if OpenAI also fails
                return new MedlinePlusResponse
                {
                    Documents = new List<MedlinePlusDocument>
                    {
                        new MedlinePlusDocument
                        {
                            Id = Guid.NewGuid().ToString(),
                            Title = $"Information about {query}",
                            Summary = "Medical information services are temporarily unavailable. Please consult with a healthcare professional for medical advice.",
                            Content = "We apologize, but both our primary medical information service and backup systems are currently unavailable. For medical questions or concerns, please contact your healthcare provider directly.",
                            DataSource = "System Message",
                            Url = "",
                            Rank = 1,
                            LastUpdated = DateTime.UtcNow,
                            Keywords = new List<string> { query },
                            AltTitles = new List<string>(),
                            GroupNames = new List<string> { "System" },
                            MeshTerms = new List<string>()
                        }
                    },
                    TotalResults = 1,
                    Term = query,
                    RetStart = 0,
                    RetMax = 1,
                    Timestamp = DateTime.UtcNow,
                    DataSource = "System"
                };
            }
        }

        /// <summary>
        /// Formats OpenAI response to match MedlinePlus response structure.
        /// </summary>
        private MedlinePlusResponse FormatOpenAiResponseAsMedlinePlus(string openAiResponse, string query, int maxResults)
        {
            var response = new MedlinePlusResponse
            {
                Documents = new List<MedlinePlusDocument>(),
                Term = query,
                RetStart = 0,
                RetMax = maxResults,
                Timestamp = DateTime.UtcNow,
                DataSource = "OpenAI"
            };

            // Split response into documents
            var documentTexts = openAiResponse.Split(new[] { "---DOCUMENT---" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < documentTexts.Length && i < maxResults; i++)
            {
                var docText = documentTexts[i].Trim();
                if (string.IsNullOrEmpty(docText)) continue;

                var document = new MedlinePlusDocument
                {
                    Id = Guid.NewGuid().ToString(),
                    Rank = i + 1,
                    LastUpdated = DateTime.UtcNow,
                    Keywords = new List<string> { query },
                    AltTitles = new List<string>(),
                    GroupNames = new List<string> { "AI Generated" },
                    MeshTerms = new List<string>(),
                    Url = $"https://ai-generated/{Guid.NewGuid()}"
                };

                // Extract title, summary, and content from the text
                var lines = docText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length > 0)
                {
                    // First non-empty line as title
                    document.Title = lines[0].Trim().TrimStart('*', '#', '-').Trim();
                }

                if (lines.Length > 1)
                {
                    // Take first 2-3 sentences as summary
                    var summaryText = string.Join(" ", lines.Skip(1));
                    var sentences = summaryText.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    document.Summary = string.Join(". ", sentences.Take(3)).Trim() + ".";

                    // Full content
                    document.Content = summaryText.Trim();
                }

                // Set a medical source with AI indicator
                var sources = new[] { "NIH (AI-Generated)", "CDC (AI-Generated)", "Mayo Clinic (AI-Generated)", "WebMD (AI-Generated)", "Medical AI Assistant" };
                document.DataSource = sources[i % sources.Length];

                response.Documents.Add(document);
            }

            response.TotalResults = response.Documents.Count;
            return response;
        }

        /// <summary>
        /// Parses the MedlinePlus XML response and extracts metadata and documents.
        /// Logs parsing steps and warnings for missing elements.
        /// </summary>
        private MedlinePlusResponse ParseMedlinePlusResponse(string xmlContent)
        {
            _logger.LogDebug("Parsing MedlinePlus XML response");
            var response = new MedlinePlusResponse
            {
                Documents = new List<MedlinePlusDocument>(),
                Timestamp = DateTime.UtcNow
            };

            var doc = XDocument.Parse(xmlContent);
            var root = doc.Element("nlmSearchResult");

            if (root == null)
            {
                _logger.LogWarning("No root element 'nlmSearchResult' found in XML response");
                return response;
            }

            // Parse search metadata
            response.TotalResults = int.Parse(root.Element("count")?.Value ?? "0");
            response.Term = root.Element("term")?.Value ?? "";
            response.RetStart = int.Parse(root.Element("retstart")?.Value ?? "0");
            response.RetMax = int.Parse(root.Element("retmax")?.Value ?? "0");

            // Parse documents
            var listElement = root.Element("list");
            if (listElement != null)
            {
                foreach (var documentElement in listElement.Elements("document"))
                {
                    var document = new MedlinePlusDocument
                    {
                        Rank = int.Parse(documentElement.Attribute("rank")?.Value ?? "0"),
                        Url = documentElement.Attribute("url")?.Value ?? "",
                        Keywords = new List<string>(),
                        AltTitles = new List<string>(),
                        GroupNames = new List<string>(),
                        MeshTerms = new List<string>()
                    };

                    // Parse content elements
                    foreach (var contentElement in documentElement.Elements("content"))
                    {
                        var name = contentElement.Attribute("name")?.Value;
                        var value = CleanHtmlContent(contentElement.Value);

                        switch (name)
                        {
                            case "title":
                                document.Title = value;
                                break;
                            case "organizationName":
                                document.DataSource = value;
                                break;
                            case "altTitle":
                                document.AltTitles.Add(value);
                                break;
                            case "FullSummary":
                                document.Content = value;
                                break;
                            case "snippet":
                                document.Summary = value;
                                break;
                            case "mesh":
                                document.MeshTerms.Add(value);
                                break;
                            case "groupName":
                                document.GroupNames.Add(value);
                                break;
                        }
                    }

                    // Generate an ID from the URL
                    document.Id = ExtractIdFromUrl(document.Url);

                    // Set last updated to current time (API doesn't provide this)
                    document.LastUpdated = DateTime.UtcNow;

                    response.Documents.Add(document);
                }
            }

            _logger.LogInformation("Parsed {Count} documents from MedlinePlus XML response", response.Documents.Count);
            return response;
        }

        // Cleans HTML content by removing tags and decoding entities.
        // Handles special span elements and common HTML entities.
        // Returns a plain text string.
        private string CleanHtmlContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return string.Empty;

            // Remove HTML tags and span elements with qt0 class
            var cleaned = Regex.Replace(content, @"<span class=""qt0"">(.*?)</span>", "$1");
            cleaned = Regex.Replace(cleaned, @"<[^>]+>", "");

            // Decode HTML entities
            cleaned = cleaned.Replace("&lt;", "<")
                           .Replace("&gt;", ">")
                           .Replace("&amp;", "&")
                           .Replace("&quot;", "\"")
                           .Replace("&#39;", "'");

            return cleaned.Trim();
        }

        // Extracts a document ID from a MedlinePlus URL.
        // Uses regex to find the page name in the URL.
        // Returns a GUID if extraction fails.
        private string ExtractIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return Guid.NewGuid().ToString();

            // Extract the page name from MedlinePlus URL as ID
            var match = Regex.Match(url, @"medlineplus\.gov/([^.]+)\.html");
            return match.Success ? match.Groups[1].Value : Guid.NewGuid().ToString();
        }
        [HttpGet("OpenAI")]
        public async Task<IActionResult> RunPythonScriptAsync([FromQuery] string query, [FromQuery] string category, [FromQuery] int maxResults)
        {
            var scriptPath = @"C:\Users\DELL\Downloads\openai_fallback_response.py"; // Use absolute path
            var args = $"{query} \"{category}\" {maxResults}";

            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\" {args}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            if (process.ExitCode != 0)
                return StatusCode(500, $"Python script error: {error}");

            return Content(output, "application/json");
        }

    }
}