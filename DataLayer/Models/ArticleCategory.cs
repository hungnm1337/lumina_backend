using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class ArticleCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int? CreatedByUserId { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<Article> Articles { get; set; } = new List<Article>();

    public virtual User? CreatedByUser { get; set; }
}
