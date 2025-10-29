using System;

namespace DataLayer.DTOs.Leaderboard
{
    public class ScoringMilestoneDTO
    {
        public int MilestoneId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MinScore { get; set; }
        public int MaxScore { get; set; }
        public string Message { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty; // "encouragement", "achievement", "warning"
        public bool IsActive { get; set; }
        public DateTime? CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
    }

    public class CreateScoringMilestoneDTO
    {
        public string Name { get; set; } = string.Empty;
        public int MinScore { get; set; }
        public int MaxScore { get; set; }
        public string Message { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class UpdateScoringMilestoneDTO : CreateScoringMilestoneDTO { }

    public class UserMilestoneNotificationDTO
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public int MilestoneId { get; set; }
        public int CurrentScore { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string MilestoneName { get; set; } = string.Empty;
    }
}


