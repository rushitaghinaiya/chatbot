    using ChatBot.Models.Entities;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using System.Text.Json.Serialization;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    namespace ChatBot.Controllers
    {
        [Route("api/[controller]")]
        [ApiController]
        public class MedlinePlusController : ControllerBase
        {
            private readonly HttpClient _httpClient;
            private readonly ILogger<MedlinePlusController> _logger;
            private const string BaseUrl = "https://wsearch.nlm.nih.gov/ws/query";

            public MedlinePlusController(HttpClient httpClient, ILogger<MedlinePlusController> logger)
            {
                _httpClient = httpClient;
                _logger = logger;
            }

            // Example: Logging without try-catch blocks for SearchHealthTopics
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

                var url = $"{BaseUrl}?db=healthTopics&term={Uri.EscapeDataString(query)}&retmax={maxResults}&rettype=brief";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = ParseMedlinePlusResponse(content);

                _logger.LogInformation("Successfully retrieved {Count} health topics for query: {Query}",
                    result.Documents?.Count ?? 0, query);

                return Ok(result);
            }

            // Example: Logging without try-catch blocks for SearchDrugs
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

                var url = $"{BaseUrl}?db=drugs&term={Uri.EscapeDataString(drugName)}&retmax={maxResults}&rettype=brief";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = ParseMedlinePlusResponse(content);

                _logger.LogInformation("Successfully retrieved {Count} drug entries for: {DrugName}",
                    result.Documents?.Count ?? 0, drugName);

                return Ok(result);
            }

            // Example: Logging without try-catch blocks for SearchEncyclopedia
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

                var url = $"{BaseUrl}?db=encyclopedia&term={Uri.EscapeDataString(topic)}&retmax={maxResults}&rettype=brief";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = ParseMedlinePlusResponse(content);

                _logger.LogInformation("Successfully retrieved {Count} encyclopedia entries for: {Topic}",
                    result.Documents?.Count ?? 0, topic);

                return Ok(result);
            }

            // Example: Logging without try-catch blocks for GetDocument
            [HttpGet("document/{documentId}")]
            public async Task<ActionResult<MedlinePlusDocument>> GetDocument(string documentId)
            {
                if (string.IsNullOrWhiteSpace(documentId))
                {
                    _logger.LogWarning("Document ID is required for document retrieval");
                    return BadRequest("Document ID is required");
                }

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

            // Example: Logging without try-catch blocks for SearchAll
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

                var url = $"{BaseUrl}?db=all&term={Uri.EscapeDataString(query)}&retmax={maxResults}&rettype=brief";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = ParseMedlinePlusResponse(content);

                _logger.LogInformation("Successfully retrieved {Count} results across all databases for: {Query}",
                    result.Documents?.Count ?? 0, query);

                return Ok(result);
            }

            // Example: Logging without try-catch blocks for HealthCheck
            [HttpGet("health-check")]
            public async Task<ActionResult<object>> HealthCheck()
            {
                var url = $"{BaseUrl}?db=healthTopics&term=diabetes&retmax=1";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("MedlinePlus API health check successful");

                return Ok(new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    ApiEndpoint = BaseUrl
                });
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
                                document.Source = value;
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
        }
    }   
