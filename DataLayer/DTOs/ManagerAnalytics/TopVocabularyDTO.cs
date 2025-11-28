namespace DataLayer.DTOs.ManagerAnalytics;

public class TopVocabularyDTO
{
    public int VocabularyListId { get; set; }
    public string ListName { get; set; } = string.Empty;
    public int LearnerCount { get; set; }
    public double AverageWordsLearned { get; set; }
    public double CompletionRate { get; set; } // percentage
    public int TotalWords { get; set; }
    public DateTime CreatedAt { get; set; }
}










