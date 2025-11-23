using System.ComponentModel.DataAnnotations;

namespace DataLayer.DTOs.Report;

// DTO cho việc tạo report mới
public class ReportCreateRequestDTO
{
    [Required]
    [MaxLength(50)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    public string ReportType { get; set; } = string.Empty; // "System", "Article", "Exam"

    public int? ArticleId { get; set; } // Required if ReportType = "Article"

    public int? ExamId { get; set; } // Required if ReportType = "Exam"
}

// DTO cho response report
public class ReportResponseDTO
{
    public int ReportId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "System", "Article-123", "Exam-456"
    public int SendBy { get; set; }
    public string SendByUserName { get; set; } = string.Empty;
    public DateTime SendAt { get; set; }
    public int? ReplyBy { get; set; }
    public string? ReplyByUserName { get; set; }
    public DateTime? ReplyAt { get; set; }
    public string? ReplyContent { get; set; }
}

// DTO cho query/pagination
public class ReportQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = "sendAt"; // sendAt | title
    public string? SortDir { get; set; } = "desc"; // asc | desc
    public string? Search { get; set; } // Search in Title and Content
    public string? Type { get; set; } // Filter by type: "System", "Article", "Exam"
    public int? ArticleId { get; set; } // Filter reports for specific article
    public int? ExamId { get; set; } // Filter reports for specific exam
    public bool? IsReplied { get; set; } // Filter by replied status
}

// DTO cho reply report
public class ReportReplyRequestDTO
{
    [Required]
    public string ReplyContent { get; set; } = string.Empty;
}

