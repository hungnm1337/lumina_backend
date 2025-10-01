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
    public List<ArticleSectionResponseDTO> Sections { get; set; } = new();
}