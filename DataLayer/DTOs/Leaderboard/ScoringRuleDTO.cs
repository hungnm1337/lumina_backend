using System;

namespace DataLayer.DTOs.Leaderboard
{
    public class ScoringRuleDTO
    {
        public int RuleId { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int BaseScore { get; set; }
        public float DifficultyMultiplier { get; set; }
        public float TimeBonusMultiplier { get; set; }
        public float AccuracyMultiplier { get; set; }
        public int MaxTimeSeconds { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
    }

    public class CreateScoringRuleDTO
    {
        public string RuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int BaseScore { get; set; }
        public float DifficultyMultiplier { get; set; }
        public float TimeBonusMultiplier { get; set; }
        public float AccuracyMultiplier { get; set; }
        public int MaxTimeSeconds { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateScoringRuleDTO : CreateScoringRuleDTO { }

    public class PracticeSessionScoreDTO
    {
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public double Accuracy { get; set; }
        public int TimeSpentSeconds { get; set; }
        public int BaseScore { get; set; }
        public int DifficultyBonus { get; set; }
        public int TimeBonus { get; set; }
        public int AccuracyBonus { get; set; }
        public int FinalScore { get; set; }
        public DateTime CompletedAt { get; set; }
        public string UserFullName { get; set; } = string.Empty;
    }
}

