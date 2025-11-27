namespace DataLayer.DTOs.ManagerAnalytics;

public class TopExamDTO
{
    public int ExamId { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public string ExamType { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public int CompletedCount { get; set; }
    public double AverageScore { get; set; }
    public double CompletionRate { get; set; } // percentage
    public DateTime CreatedAt { get; set; }
}








