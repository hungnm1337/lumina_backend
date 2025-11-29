using System;

namespace DataLayer.DTOs.Streak
{
    
    public class StreakSummaryDTO
    {
        public int CurrentStreak { get; set; }

        public int LongestStreak { get; set; }

        public bool TodayCompleted { get; set; }

        public int FreezeTokens { get; set; }

        public int? LastMilestone { get; set; }

        public DateOnly? LastPracticeDate { get; set; }

        public int? NextMilestone { get; set; }

        public int? DaysToNextMilestone { get; set; }
    }
}