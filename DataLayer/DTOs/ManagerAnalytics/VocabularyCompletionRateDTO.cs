namespace DataLayer.DTOs.ManagerAnalytics;

public class VocabularyCompletionRateDTO
{
    public int VocabularyListId { get; set; }
    public string ListName { get; set; } = string.Empty;
    public int TotalLearners { get; set; }
    public int CompletedLearners { get; set; }
    public double CompletionRate { get; set; } 
    public double AverageWordsLearned { get; set; }
    public int TotalWords { get; set; }
}
















