using System;
using System.Collections.Generic;

namespace lumina.Models;

public partial class Prompt
{
    public int PromptId { get; set; }

    public int? PassageId { get; set; }

    public string Skill { get; set; } = null!;

    public string? PromptText { get; set; }

    public string? ReferenceImageUrl { get; set; }

    public string? ReferenceAudioUrl { get; set; }

    public virtual Passage? Passage { get; set; }

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
