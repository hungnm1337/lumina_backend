using System;

namespace DataLayer.DTOs.Vocabulary
{
    public class SpacedRepetitionDTO
    {
        public int UserSpacedRepetitionId { get; set; }
        public int UserId { get; set; }
        public int VocabularyListId { get; set; }
        public string VocabularyListName { get; set; } = string.Empty;
        public DateTime LastReviewedAt { get; set; }
        public DateTime? NextReviewAt { get; set; }
        public int ReviewCount { get; set; }
        public int Intervals { get; set; } // Số ngày
        public string Status { get; set; } = string.Empty;
        public bool IsDue { get; set; }
        public int DaysUntilReview { get; set; }
        
        // Quiz score fields
        public int? BestQuizScore { get; set; }
        public int? LastQuizScore { get; set; }
        public DateTime? LastQuizCompletedAt { get; set; }
        public int? TotalQuizAttempts { get; set; }
    }

    public class ReviewVocabularyRequestDTO
    {
        public int UserSpacedRepetitionId { get; set; }
        public int Quality { get; set; } // 0-5: 0 = không nhớ, 5 = nhớ rất tốt
    }

    public class ReviewVocabularyResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public SpacedRepetitionDTO? UpdatedRepetition { get; set; }
        public DateTime? NextReviewAt { get; set; }
        public int NewIntervals { get; set; }
    }

    // DTO for saving quiz result
    public class SaveQuizResultRequestDTO
    {
        public int VocabularyListId { get; set; }
        public int Score { get; set; } // 0-100
        public int TotalQuestions { get; set; }
        public int CorrectCount { get; set; }
        public int TotalTimeSpent { get; set; } // seconds
        public string Mode { get; set; } = string.Empty; // practice, test, challenge
    }

    // DTO for returning quiz scores
    public class QuizScoreDTO
    {
        public int VocabularyListId { get; set; }
        public string VocabularyListName { get; set; } = string.Empty;
        public int? BestScore { get; set; }
        public int? LastScore { get; set; }
        public DateTime? LastCompletedAt { get; set; }
        public int? TotalAttempts { get; set; }
    }
}

