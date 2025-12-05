namespace DataLayer.DTOs.ManagerAnalytics;

public class ArticleCompletionRateDTO
{
    public int ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TotalReaders { get; set; }
    public int CompletedReaders { get; set; }
    public double CompletionRate { get; set; } 
    public double AverageReadingTime { get; set; } 
}
























