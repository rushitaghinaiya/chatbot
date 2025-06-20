using ChatBot.Models.ViewModels;

namespace ChatBot.Models.Services
{
    public interface IAdmin
    {
        int SaveFileMetadataToDatabase(UploadFile file);
    }
}
