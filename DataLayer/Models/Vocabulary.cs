using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class Vocabulary
{
    public int VocabularyId { get; set; }

    public int VocabularyListId { get; set; }

    public string Word { get; set; } = null!;

    public string Definition { get; set; } = null!;

    public string? Example { get; set; }

    public string TypeOfWord { get; set; } = null!;

    public string? Category { get; set; }

    public bool? IsDeleted { get; set; }

    public string? ImageUrl { get; set; } // URL ảnh từ Cloudinary cho từng vocabulary

    public virtual VocabularyList VocabularyList { get; set; } = null!;
}
