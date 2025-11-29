using System;

namespace DataLayer.DTOs.Streak
{
    
    public class StreakReminderDTO
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int CurrentStreak { get; set; }
        public int FreezeTokens { get; set; }
        public string ReminderMessage { get; set; } = string.Empty;
        public DateOnly ReminderDate { get; set; }
    }

   
    public class ReminderJobResultDTO
    {
        public bool Success { get; set; }
        public int TotalUsers { get; set; }
        public int EmailsSent { get; set; }
        public int Errors { get; set; }
        public double DurationMs { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}