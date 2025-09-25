using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class ArticleSection
{
    public int SectionId { get; set; }

    public int ArticleId { get; set; }

    public string SectionTitle { get; set; } = null!;

    public string SectionContent { get; set; } = null!;

    public int OrderIndex { get; set; }

    public virtual Article Article { get; set; } = null!;

    public virtual ICollection<SectionCompletion> SectionCompletions { get; set; } = new List<SectionCompletion>();

    public virtual ICollection<UserNote> UserNotes { get; set; } = new List<UserNote>();
}
