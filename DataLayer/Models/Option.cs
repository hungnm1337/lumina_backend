using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class Option
{
    public int OptionId { get; set; }

    public int QuestionId { get; set; }

    public string Content { get; set; } = null!;

    public bool? IsCorrect { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual ICollection<UserAnswerMultipleChoice> UserAnswerMultipleChoices { get; set; } = new List<UserAnswerMultipleChoice>();
}
