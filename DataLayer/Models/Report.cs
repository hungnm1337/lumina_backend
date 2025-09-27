using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class Report
{
    public int ReportId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public int SendBy { get; set; }

    public DateTime SendAt { get; set; }

    public int? ReplyBy { get; set; }

    public DateTime? ReplyAt { get; set; }

    public string? ReplyContent { get; set; }

    public string Type { get; set; } = null!;

    public virtual User SendByNavigation { get; set; } = null!;
}
