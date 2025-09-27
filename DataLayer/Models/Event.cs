using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class Event
{
    public int EventId { get; set; }

    public string EventName { get; set; } = null!;

    public string? Content { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public int CreateBy { get; set; }

    public int? UpdateBy { get; set; }

    public virtual User CreateByNavigation { get; set; } = null!;

    public virtual User? UpdateByNavigation { get; set; }
}
