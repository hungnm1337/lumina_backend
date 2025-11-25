namespace DataLayer.DTOs.ManagerAnalytics;

public class TopSlideDTO
{
    public int SlideId { get; set; }
    public string SlideName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

