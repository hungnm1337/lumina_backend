namespace DataLayer.DTOs.ManagerAnalytics;

public class ActiveUsersDTO
{
    public int ActiveUsersNow { get; set; }
    public int TotalUsers { get; set; }
    public int NewUsersThisWeek { get; set; }
    public int NewUsersThisMonth { get; set; }
    public double GrowthRate { get; set; }
}








