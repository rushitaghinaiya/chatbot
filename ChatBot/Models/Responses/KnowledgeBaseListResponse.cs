using System.Text.Json.Serialization;

namespace ChatBot.Models.Responses
{
    public class KnowledgeBaseListResponse
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("knowledgebase_list")]
        public List<KnowledgeBaseCategoryInfo> KnowledgebaseList { get; set; } = new();
    }

    public class KnowledgeBaseInfo
    {
        [JsonPropertyName("kb_name")]
        public string KbName { get; set; } = string.Empty;

        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;

        [JsonPropertyName("document_count")]
        public int DocumentCount { get; set; }

        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; } = new();

        [JsonPropertyName("created_date")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("last_updated")]
        public DateTime LastUpdated { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class KnowledgeBaseCategoryInfo
    {
        [JsonPropertyName("document_category")]
        public string DocumentCategory { get; set; } = string.Empty;

        [JsonPropertyName("processed_files_count")]
        public int ProcessedFilesCount { get; set; }

        [JsonPropertyName("processed_files")]
        public List<string> ProcessedFiles { get; set; } = new();

        [JsonPropertyName("unprocessed_files_count")]
        public int UnprocessedFilesCount { get; set; }

        [JsonPropertyName("unprocessed_files")]
        public List<string> UnprocessedFiles { get; set; } = new();

        [JsonPropertyName("last_updated")]
        public string LastUpdated { get; set; } = string.Empty;
    }
}
