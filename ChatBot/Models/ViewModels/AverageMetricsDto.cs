namespace ChatBot.Models.ViewModels
{
    public class AverageMetricsDto
    {
        public string AvgSessionDuration { get; set; }  // e.g. "18 min"
        public double AvgQueriesPerUser { get; set; }   // e.g. 6.2
        public double AvgFamilyMembersPerUser { get; set; }  // e.g. 1.8
    }

}
