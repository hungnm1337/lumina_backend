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
    public int? CreatedBy { get; set; } // Filter by author/user ID
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

public class ToggleHideRequest
{
    [Required]
    public bool IsPublished { get; set; }
}

// DTO cho việc tạo category mới
public class CategoryCreateDTO
{
    [Required]
    [MaxLength(255)]
    [MinLength(2)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

// DTO cho response category
public class CategoryResponseDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreateAt { get; set; }
}

// DTOs cho Article Progress
public class ArticleProgressRequestDTO
{
    public int ProgressPercent { get; set; }
    public string Status { get; set; } = string.Empty; // "not_started" | "in_progress" | "completed"
}

public class ArticleProgressResponseDTO
{
    public int ArticleId { get; set; }
    public int ProgressPercent { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastAccessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}