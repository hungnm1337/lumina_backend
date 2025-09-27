using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class ExamAttempt
{
    public int AttemptId { get; set; }

    public int UserId { get; set; }

    public int ExamId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int? Score { get; set; }

    public string Status { get; set; } = null!;

    public int? ListeningScore { get; set; }

    public int? ReadingScore { get; set; }

    public int? SpeakingScore { get; set; }

    public int? WritingScore { get; set; }

    public virtual Exam Exam { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}
