namespace ChatBot.Models.ViewModels
{
    public class QueryTopicDistributionDto
    {
        public string Topic { get; set; }
        public int QueryCount { get; set; }
        public double Percentage { get; set; }
    }

}
