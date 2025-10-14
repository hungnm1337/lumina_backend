using System;
using System.Collections.Generic;

namespace lumina.Models;

public partial class Article
{
    public int ArticleId { get; set; }

    public int CategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string Summary { get; set; } = null!;

    public bool? IsPublished { get; set; }

    public int CreatedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<ArticleSection> ArticleSections { get; set; } = new List<ArticleSection>();

    public virtual ArticleCategory Category { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }

    public virtual ICollection<UserArticleProgress> UserArticleProgresses { get; set; } = new List<UserArticleProgress>();

    public virtual ICollection<UserNote> UserNotes { get; set; } = new List<UserNote>();
}
