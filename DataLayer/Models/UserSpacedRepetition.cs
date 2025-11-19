using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class UserSpacedRepetition
{
    public int UserSpacedRepetitionId { get; set; }

    public int UserId { get; set; }

    public int VocabularyListId { get; set; }

    public DateTime LastReviewedAt { get; set; }

    public DateTime? NextReviewAt { get; set; }

    public int? ReviewCount { get; set; }

    public int? Intervals { get; set; }

    public string? Status { get; set; }

    // Quiz score fields
    public int? BestQuizScore { get; set; } // Điểm cao nhất (0-100)
    public int? LastQuizScore { get; set; } // Điểm lần làm gần nhất (0-100)
    public DateTime? LastQuizCompletedAt { get; set; } // Thời gian làm quiz gần nhất
    public int? TotalQuizAttempts { get; set; } // Tổng số lần làm quiz

    public virtual User User { get; set; } = null!;

    public virtual VocabularyList VocabularyList { get; set; } = null!;
}
