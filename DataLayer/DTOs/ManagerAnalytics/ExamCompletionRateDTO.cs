namespace DataLayer.DTOs.ManagerAnalytics;

public class ExamCompletionRateDTO
{
    public int ExamId { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public string ExamType { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int CompletedAttempts { get; set; }
    public double CompletionRate { get; set; } 
    public double AverageCompletionTime { get; set; }
}





















