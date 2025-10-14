using System;
using System.Collections.Generic;

namespace lumina.Models;

public partial class Package
{
    public int PackageId { get; set; }

    public string PackageName { get; set; } = null!;

    public decimal? Price { get; set; }

    public int? DurationInDays { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
