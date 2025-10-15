using System.ComponentModel.DataAnnotations;

namespace DataLayer.DTOs.Article;

// DTO cho việc tạo một section mới
public class ArticleSectionCreateDTO
{
    [Required]
    [MaxLength(255)]
    public string SectionTitle { get; set; } = string.Empty;

    [Required]
    public string SectionContent { get; set; } = string.Empty;

    [Required]
    public int OrderIndex { get; set; }
}

// DTO cho việc tạo một article mới
public class ArticleCreateDTO
{
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public int CategoryId { get; set; }

    // true nếu nhấn "Lưu & Xuất bản", false nếu nhấn "Lưu nháp"
    public bool PublishNow { get; set; } = false;

    public List<ArticleSectionCreateDTO> Sections { get; set; } = new();
}

// ----- DTOs cho Response (trả về cho client) -----

public class ArticleSectionResponseDTO
{
    public int SectionId { get; set; }
    public string SectionTitle { get; set; } = string.Empty;
    public string SectionContent { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

public class ArticleResponseDTO
{
    public int ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public bool? IsPublished { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public List<ArticleSectionResponseDTO> Sections { get; set; } = new();
}

// ----- Update / Publish / Query DTOs -----

public class ArticleSectionUpdateDTO
{
    public int? SectionId { get; set; }
    public string SectionTitle { get; set; } = string.Empty;
    public string SectionContent { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

public class ArticleUpdateDTO
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public List<ArticleSectionUpdateDTO> Sections { get; set; } = new();
}

public class ArticlePublishRequest
{
    public bool Publish { get; set; } = true;
}

public class ArticleQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = "createdAt"; // createdAt | title | category
    public string? SortDir { get; set; } = "desc"; // asc | desc
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public bool? IsPublished { get; set; }
    public string? Status { get; set; }
}

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
public class ArticleReviewRequest
{
    [Required]
    public bool IsApproved { get; set; }
    public string? Comment { get; set; }
}