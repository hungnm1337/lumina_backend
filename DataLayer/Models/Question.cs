using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class Question
{
    public int QuestionId { get; set; }

    public int PartId { get; set; }

    public string QuestionType { get; set; } = null!;

    public string StemText { get; set; } = null!;

    public int? PromptId { get; set; }

    public int ScoreWeight { get; set; }

    public string? QuestionExplain { get; set; }

    public int Time { get; set; }

    public int QuestionNumber { get; set; }

    public virtual ICollection<Option> Options { get; set; } = new List<Option>();

    public virtual ExamPart Part { get; set; } = null!;

    public virtual Prompt? Prompt { get; set; }

    public virtual ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}
