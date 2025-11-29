namespace DataLayer.DTOs.Streak
{
    
    public class StreakUpdateResultDTO
    {
        public bool Success { get; set; }

        public StreakSummaryDTO? Summary { get; set; }

        public StreakEventType EventType { get; set; }

        public bool MilestoneReached { get; set; }

        public int? MilestoneValue { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}