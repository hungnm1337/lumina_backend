using System;
using System.Collections.Generic;

namespace lumina.Models;

public partial class UserArticleProgress
{
    public int ProgressId { get; set; }

    public int UserId { get; set; }

    public int ArticleId { get; set; }

    public int? ProgressPercent { get; set; }

    public string? Status { get; set; }

    public DateTime LastAccessedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Article Article { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
