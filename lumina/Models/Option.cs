using System;
using System.Collections.Generic;

namespace lumina.Models;

public partial class Option
{
    public int OptionId { get; set; }

    public int QuestionId { get; set; }

    public string Content { get; set; } = null!;

    public bool? IsCorrect { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}
