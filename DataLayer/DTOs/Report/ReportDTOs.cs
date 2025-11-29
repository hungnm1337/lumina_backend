using System.ComponentModel.DataAnnotations;

namespace DataLayer.DTOs.Report;

public class ReportCreateRequestDTO
{
    [Required]
    [MaxLength(50)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public string ReportType { get; set; } = string.Empty;

    public int? ArticleId { get; set; }

    public int? ExamId { get; set; }
}

public class ReportResponseDTO
{
    public int ReportId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int SendBy { get; set; }
    public string SendByUserName { get; set; } = string.Empty;
    public DateTime SendAt { get; set; }
    public int? ReplyBy { get; set; }
    public string? ReplyByUserName { get; set; }
    public DateTime? ReplyAt { get; set; }
    public string? ReplyContent { get; set; }
}

public class ReportQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = "sendAt";
    public string? SortDir { get; set; } = "desc";
    public string? Search { get; set; }
    public string? Type { get; set; }
    public int? ArticleId { get; set; }
    public int? ExamId { get; set; }
    public bool? IsReplied { get; set; }
}

public class ReportReplyRequestDTO
{
    [Required]
    public string ReplyContent { get; set; } = string.Empty;
}