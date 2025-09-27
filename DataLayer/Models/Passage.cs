using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class Passage
{
    public int PassageId { get; set; }

    public string ContentText { get; set; } = null!;

    public string Title { get; set; } = null!;

    public virtual ICollection<Prompt> Prompts { get; set; } = new List<Prompt>();
}
