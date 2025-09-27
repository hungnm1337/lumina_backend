using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class SectionCompletion
{
    public int CompletionId { get; set; }

    public int UserId { get; set; }

    public int SectionId { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual ArticleSection Section { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
