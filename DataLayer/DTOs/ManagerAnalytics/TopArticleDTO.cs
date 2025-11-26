namespace DataLayer.DTOs.ManagerAnalytics;

public class TopArticleDTO
{
    public int ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public int ReaderCount { get; set; }
    public double AverageReadingTime { get; set; } // in minutes
    public double CompletionRate { get; set; } // percentage
    public DateTime CreatedAt { get; set; }
}



