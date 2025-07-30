using System.Text.Json.Serialization;

namespace ChatBot.Models.Responses
{
    public class DeleteFileResponse
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        // These properties are assumed - update based on actual API response
        [JsonPropertyName("deleted_files")]
        public List<string> DeletedFiles { get; set; } = new();

        [JsonPropertyName("failed_files")]
        public List<FailedFileInfo> FailedFiles { get; set; } = new();

        [JsonPropertyName("deleted_count")]
        public int DeletedCount { get; set; }

        [JsonPropertyName("failed_count")]
        public int FailedCount { get; set; }
    }

    public class FailedFileInfo
    {
        [JsonPropertyName("file_name")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("error_reason")]
        public string ErrorReason { get; set; } = string.Empty;
    }
}
