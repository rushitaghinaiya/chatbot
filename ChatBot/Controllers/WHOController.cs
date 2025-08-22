using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ChatBot.Controllers
{
    [ApiController]
    [Route("v1/[controller]/[action]")]
    [EnableCors("allowCors")]
    [Produces("application/json")]
    public class WHOController : Controller
    {
        private readonly HttpClient _http;

        public WHOController(HttpClient httpClient)
        {
            _http = httpClient;
        }

        // ✅ Life Expectancy
        [HttpGet("life-expectancy/{countryCode}")]
        public async Task<IActionResult> GetLifeExpectancy(string countryCode)
        {
            var url = $"https://ghoapi.azureedge.net/api/WHOSIS_000001?$filter=SpatialDim eq '{countryCode}'";
            var result = await _http.GetStringAsync(url);
            return Content(result, "application/json");
        }

        // ✅ Health Topics (search by keyword)
        [HttpGet("topics")]
        public async Task<IActionResult> GetTopics([FromQuery] string search)
        {
            var url = $"https://www.who.int/api/news/healthtopics?$filter=contains(tolower(Title),'{search.ToLower()}')";
            var result = await _http.GetStringAsync(url);
            return Content(result, "application/json");
        }

        // ✅ Topic News
        [HttpGet("topic-news/{topicId}")]
        public async Task<IActionResult> GetTopicNews(string topicId)
        {
            var url = $"https://www.who.int/api/news/newsitems?$filter=healthtopics/any(h: h eq {topicId})&$orderby=PublicationDateAndTime desc";
            var result = await _http.GetStringAsync(url);
            return Content(result, "application/json");
        }

        // ✅ Topic Related Statistics
        [HttpGet("topic-stats/{topicId}")]
        public async Task<IActionResult> GetTopicStats(string topicId)
        {
            var url = $"https://www.who.int/api/news/healthtopics({topicId})/RelatedStatistics";
            var result = await _http.GetStringAsync(url);
            return Content(result, "application/json");
        }

        // ✅ Topic Fact Sheets
        [HttpGet("topic-facts/{topicId}")]
        public async Task<IActionResult> GetTopicFacts(string topicId)
        {
            var url = $"https://www.who.int/api/hubs/factsheets?$filter=healthtopics/any(h: h eq {topicId})";
            var result = await _http.GetStringAsync(url);
            return Content(result, "application/json");
        }
    }
}
