using System;
using System.Collections.Generic;

namespace lumina.Models;

public partial class Slide
{
    public int SlideId { get; set; }

    public string SlideUrl { get; set; } = null!;

    public string SlideName { get; set; } = null!;

    public int? UpdateBy { get; set; }

    public int CreateBy { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? UpdateAt { get; set; }

    public DateTime CreateAt { get; set; }

    public virtual User CreateByNavigation { get; set; } = null!;

    public virtual User? UpdateByNavigation { get; set; }
}
