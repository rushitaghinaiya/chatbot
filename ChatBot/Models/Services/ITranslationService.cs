namespace ChatBot.Models.Services
{
    public interface ITranslationService
    {
        Task<string?> TranslateAsync(string text, string targetLanguage);
    }
}
