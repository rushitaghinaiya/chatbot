using ChatBot.Models.Configuration;
using ChatBot.Models.Services;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace ChatBot.Repository
{
    public class TranslationService : ITranslationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GoogleTranslateOptions _options;
        private readonly ILogger<TranslationService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public TranslationService(
            IHttpClientFactory httpClientFactory,
            IOptions<GoogleTranslateOptions> options,
            ILogger<TranslationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string?> TranslateAsync(string text, string targetLanguage)
        {
            var url = $"https://translation.googleapis.com/language/translate/v2?key={_options.ApiKey}";

            var body = new
            {
                q = text,
                target = targetLanguage,
                format = "text"
            };

            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient("GoogleTranslate");

            try
            {
                var response = await client.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Google Translate API returned {StatusCode}: {Body}", response.StatusCode, responseBody);
                    return null;
                }

                using var doc = JsonDocument.Parse(responseBody);
                var translatedText = doc
                    .RootElement
                    .GetProperty("data")
                    .GetProperty("translations")[0]
                    .GetProperty("translatedText")
                    .GetString();

                return translatedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Google Translate API");
                return null;
            }
        }
    }
}
