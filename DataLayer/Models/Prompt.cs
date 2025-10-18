using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class Prompt
{
    public int PromptId { get; set; }

    public string Skill { get; set; } = null!;

    public string ContentText { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? ReferenceImageUrl { get; set; }

    public string? ReferenceAudioUrl { get; set; }


    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
