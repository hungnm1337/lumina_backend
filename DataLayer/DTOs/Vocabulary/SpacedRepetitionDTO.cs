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
}

