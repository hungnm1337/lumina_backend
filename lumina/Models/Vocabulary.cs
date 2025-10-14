using System;
using System.Collections.Generic;

namespace lumina.Models;

public partial class Vocabulary
{
    public int VocabularyId { get; set; }

    public int VocabularyListId { get; set; }

    public string Word { get; set; } = null!;

    public string Definition { get; set; } = null!;

    public string? Example { get; set; }

    public string TypeOfWord { get; set; } = null!;

    public bool? IsDeleted { get; set; }

    public virtual VocabularyList VocabularyList { get; set; } = null!;
}
