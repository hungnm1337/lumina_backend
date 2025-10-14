using System;
using System.Collections.Generic;

namespace lumina.Models;

public partial class UserNote
{
    public int NoteId { get; set; }

    public int UserId { get; set; }

    public int ArticleId { get; set; }

    public int SectionId { get; set; }

    public string NoteContent { get; set; } = null!;

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual Article Article { get; set; } = null!;

    public virtual ArticleSection Section { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
