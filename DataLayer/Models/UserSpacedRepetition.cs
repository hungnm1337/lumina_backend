using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class UserSpacedRepetition
{
    public int UserSpacedRepetitionId { get; set; }

    public int UserId { get; set; }

    public int? VocabularyId { get; set; }

    public int VocabularyListId { get; set; }

    public DateTime LastReviewedAt { get; set; }

    public DateTime? NextReviewAt { get; set; }

    public int? ReviewCount { get; set; }

    public int? Intervals { get; set; }

    public string? Status { get; set; }

    public int? BestQuizScore { get; set; }
    public int? LastQuizScore { get; set; }
    public DateTime? LastQuizCompletedAt { get; set; }
    public int? TotalQuizAttempts { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual VocabularyList VocabularyList { get; set; } = null!;

    public virtual Vocabulary? Vocabulary { get; set; }
}