using System;

namespace DataLayer.DTOs.Leaderboard
{
    public class LeaderboardDTO
    {
        public int LeaderboardId { get; set; }
        public string? SeasonName { get; set; }
        public int SeasonNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
        
        // Thông tin bổ sung cho Season
        public int TotalParticipants { get; set; }
        public string Status { get; set; } = "Upcoming"; // Upcoming, Active, Ended
        public int DaysRemaining { get; set; }
    }

    public class CreateLeaderboardDTO
    {
        public string? SeasonName { get; set; }
        public int SeasonNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateLeaderboardDTO
    {
        public string? SeasonName { get; set; }
        public int SeasonNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class LeaderboardRankDTO
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int Rank { get; set; }
        public int? EstimatedTOEICScore { get; set; } // Điểm TOEIC ước tính (0-990)
        public string ToeicLevel { get; set; } = string.Empty; // Beginner, Elementary, Intermediate, etc.
        public string? AvatarUrl { get; set; }
    }

    // DTO cho tính điểm theo mốc TOEIC
    public class TOEICScoreCalculationDTO
    {
        public int UserId { get; set; }
        public int EstimatedTOEICScore { get; set; } // 0-990
        public string ToeicLevel { get; set; } = string.Empty;
        public int BasePointsPerCorrect { get; set; }
        public decimal TimeBonus { get; set; }
        public decimal AccuracyBonus { get; set; }
        public decimal DifficultyMultiplier { get; set; }
        public int TotalSeasonScore { get; set; }
    }

    // DTO cho reset season
    public class ResetSeasonDTO
    {
        public int LeaderboardId { get; set; }
        public bool ArchiveScores { get; set; } = true; // Lưu trữ điểm cũ trước khi reset
    }

    // DTO cho thông tin user trong season hiện tại
    public class UserSeasonStatsDTO
    {
        public int UserId { get; set; }
        public int CurrentRank { get; set; }
        public int CurrentScore { get; set; }
        public int EstimatedTOEICScore { get; set; }
        public string ToeicLevel { get; set; } = string.Empty;
        public int TotalAttempts { get; set; }
        public int CorrectAnswers { get; set; }
        public decimal AccuracyRate { get; set; }
        public bool IsReadyForTOEIC { get; set; } // >= 600 điểm
    }
}
