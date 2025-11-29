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
        public string Status { get; set; } = "Upcoming"; 
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
        public int? EstimatedTOEICScore { get; set; } 
        public string ToeicLevel { get; set; } = string.Empty; 
        public string? AvatarUrl { get; set; }
    }

    public class TOEICScoreCalculationDTO
    {
        public int UserId { get; set; }
        public int EstimatedTOEICScore { get; set; } 
        public string ToeicLevel { get; set; } = string.Empty;
        public int BasePointsPerCorrect { get; set; }
        public decimal TimeBonus { get; set; }
        public decimal AccuracyBonus { get; set; }
        public decimal DifficultyMultiplier { get; set; }
        public int TotalSeasonScore { get; set; }
    }

    public class ResetSeasonDTO
    {
        public int LeaderboardId { get; set; }
        public bool ArchiveScores { get; set; } = true; 
    }

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
        public bool IsReadyForTOEIC { get; set; } 
    }

    public class CalculateScoreRequestDTO
    {
        public int ExamAttemptId { get; set; }
        public int ExamPartId { get; set; } 
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public int TimeSpentSeconds { get; set; }
        public int ExpectedTimeSeconds { get; set; }
    }

    public class CalculateScoreResponseDTO
    {
        public int SeasonScore { get; set; } 
        public int EstimatedTOEIC { get; set; } 
        public string TOEICLevel { get; set; } = string.Empty; 
        public int BasePoints { get; set; }
        public int TimeBonus { get; set; }
        public int AccuracyBonus { get; set; }
        public bool IsFirstAttempt { get; set; } 
        public string? TOEICMessage { get; set; } 
        public int TotalAccumulatedScore { get; set; } 
    }

    public class TOEICLevelConfig
    {
        public string Level { get; set; } = string.Empty;
        public int MinScore { get; set; }
        public int MaxScore { get; set; }
        public int BasePointsPerCorrect { get; set; }
        public double TimeBonusPercent { get; set; }
        public double AccuracyBonusPercent { get; set; }
    }
}
