namespace DataLayer.DTOs.ManagerAnalytics;

public class TopEventDTO
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int ParticipantCount { get; set; }
    public double ParticipationRate { get; set; } // percentage (if we can calculate)
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
}








