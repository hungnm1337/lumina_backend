using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class Subscription
{
    public int SubscriptionId { get; set; }

    public int UserId { get; set; }

    public int PackageId { get; set; }

    public int PaymentId { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? Status { get; set; }

    public virtual Package Package { get; set; } = null!;

    public virtual Payment Payment { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
