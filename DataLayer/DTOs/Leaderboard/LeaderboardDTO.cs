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

    // DTO cho request tính điểm season
    public class CalculateScoreRequestDTO
    {
        public int ExamAttemptId { get; set; }
        public int ExamPartId { get; set; } // 1=Listening, 2=Reading
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public int TimeSpentSeconds { get; set; }
        public int ExpectedTimeSeconds { get; set; }
    }

    // DTO cho response tính điểm season
    public class CalculateScoreResponseDTO
    {
        public int SeasonScore { get; set; } // Điểm tích lũy mỗi lần làm bài
        public int EstimatedTOEIC { get; set; } // Điểm TOEIC ước tính (0-990) - chỉ cập nhật khi làm đề lần đầu
        public string TOEICLevel { get; set; } = string.Empty; // Trình độ: Beginner, Elementary, Intermediate, Upper-Intermediate, Advanced, Proficient
        public int BasePoints { get; set; }
        public int TimeBonus { get; set; }
        public int AccuracyBonus { get; set; }
        public bool IsFirstAttempt { get; set; } // True nếu là lần đầu tiên làm ĐỀ này (Exam + Part)
        public string? TOEICMessage { get; set; } // Thông báo trình độ - hiển thị mỗi lần
        public int TotalAccumulatedScore { get; set; } // Tổng điểm tích lũy hiện tại
        
        // Metadata for frontend display
        public bool ShowTOEICInfo { get; set; } = false; // Control whether to show TOEIC section
        public string DisplayLanguage { get; set; } = "en"; // "en" or "vi"
    }

    // DTO cho cấu hình level TOEIC
    public class TOEICLevelConfig
    {
        public string Level { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty; // Mô tả khả năng
        public int MinScore { get; set; }
        public int MaxScore { get; set; }
        public int BasePointsPerCorrect { get; set; }
        public double TimeBonusPercent { get; set; }
        public double AccuracyBonusPercent { get; set; }
    }
}
