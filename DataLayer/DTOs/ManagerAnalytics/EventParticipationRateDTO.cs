namespace DataLayer.DTOs.ManagerAnalytics;

public class EventParticipationRateDTO
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int TotalUsers { get; set; } // Total users in system or invited
    public int Participants { get; set; }
    public double ParticipationRate { get; set; } // percentage
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}






