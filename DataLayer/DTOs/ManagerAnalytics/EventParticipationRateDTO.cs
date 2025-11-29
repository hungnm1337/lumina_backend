namespace DataLayer.DTOs.ManagerAnalytics;

public class EventParticipationRateDTO
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int TotalUsers { get; set; } 
    public int Participants { get; set; }
    public double ParticipationRate { get; set; } 
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}










