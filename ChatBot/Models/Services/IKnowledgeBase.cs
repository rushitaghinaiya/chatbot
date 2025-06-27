using ChatBot.Models.Entities;
using ChatBot.Models.ViewModels;

namespace ChatBot.Models.Services
{
    public interface IKnowledgeBase
    {
        Task<FileQnAVM> ProcessQuestionAsync(
           string companyCode,
           FileQnARequest request);

        Task<StoreFileVM> StoreFilesAsync(
            string companyCode,
            StoreFileRequest request);

        Task<bool> ValidateCompanyCodeAsync(string companyCode);
        Task<bool> ValidateKnowledgeBaseAsync(string companyCode, string kbName);
    }
}
