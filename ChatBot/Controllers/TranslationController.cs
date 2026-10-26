using ChatBot.Models.Responses;
using ChatBot.Models.Services;
using ChatBot.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChatBot.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class TranslationController : ControllerBase
    {
        private readonly ITranslationService _translationService;
        private readonly ILogger<TranslationController> _logger;

        public TranslationController(
            ITranslationService translationService,
            ILogger<TranslationController> logger)
        {
            _translationService = translationService;
            _logger = logger;
        }

        [HttpPost("translate")]
        [ProducesResponseType(typeof(ApiResponseVM<TranslationResponse>), 200)]
        public async Task<IActionResult> Translate(
            [FromQuery] string text,
            [FromQuery] string target)
        {
            _logger.LogInformation("Translation request received for target language: {Target}", target);

            var translatedText = await _translationService.TranslateAsync(text, target);

            if (translatedText == null)
            {
                return Ok(new ApiResponseVM<TranslationResponse>
                {
                    Success = false,
                    Data = new TranslationResponse { TranslatedText = string.Empty }
                });
            }

            return Ok(new ApiResponseVM<TranslationResponse>
            {
                Success = true,
                Data = new TranslationResponse { TranslatedText = translatedText }
            });
        }
    }
}
