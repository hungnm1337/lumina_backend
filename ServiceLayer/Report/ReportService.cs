using DataLayer.DTOs;
using DataLayer.DTOs.Report;
using DataLayer.Models;
using Microsoft.Extensions.Logging;
using RepositoryLayer.Exam;
using RepositoryLayer.Report;
using RepositoryLayer.UnitOfWork;

namespace ServiceLayer.Report;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReportRepository _reportRepository;
    private readonly IExamRepository _examRepository;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IUnitOfWork unitOfWork, 
        IReportRepository reportRepository,
        IExamRepository examRepository,
        ILogger<ReportService> logger)
    {
        _unitOfWork = unitOfWork;
        _reportRepository = reportRepository;
        _examRepository = examRepository;
        _logger = logger;
    }

    public async Task<ReportResponseDTO> CreateReportAsync(ReportCreateRequestDTO request, int userId)
    {
        // Validate ReportType
        if (string.IsNullOrWhiteSpace(request.ReportType))
        {
            throw new ArgumentException("ReportType is required.");
        }

        string typeValue;
        
        // Format Type field based on ReportType
        if (request.ReportType.Equals("System", StringComparison.OrdinalIgnoreCase))
        {
            typeValue = "System";
        }
        else if (request.ReportType.Equals("Article", StringComparison.OrdinalIgnoreCase))
        {
            if (!request.ArticleId.HasValue)
            {
                throw new ArgumentException("ArticleId is required when ReportType is Article.");
            }
            
            // Validate Article exists
            var article = await _unitOfWork.Articles.FindByIdAsync(request.ArticleId.Value);
            if (article == null)
            {
                throw new KeyNotFoundException($"Article with ID {request.ArticleId.Value} not found.");
            }
            
            typeValue = $"Article-{request.ArticleId.Value}";
        }
        else if (request.ReportType.Equals("Exam", StringComparison.OrdinalIgnoreCase))
        {
            if (!request.ExamId.HasValue)
            {
                throw new ArgumentException("ExamId is required when ReportType is Exam.");
            }
            
            // Validate Exam exists
            var exam = await _examRepository.GetExamDetailAndExamPartByExamID(request.ExamId.Value);
            if (exam == null)
            {
                throw new KeyNotFoundException($"Exam with ID {request.ExamId.Value} not found.");
            }
            
            typeValue = $"Exam-{request.ExamId.Value}";
        }
        else
        {
            throw new ArgumentException($"Invalid ReportType: {request.ReportType}. Must be 'System', 'Article', or 'Exam'.");
        }

        var report = new Report
        {
            Title = request.Title,
            Content = request.Content,
            Type = typeValue,
            SendBy = userId,
            SendAt = DateTime.UtcNow
        };

        await _reportRepository.AddAsync(report);
        await _unitOfWork.CompleteAsync();

        // Get user info for response
        var user = await _unitOfWork.Users.GetUserByIdAsync(userId);
        
        _logger.LogInformation("Report {ReportId} created by User {UserId}", report.ReportId, userId);

        return new ReportResponseDTO
        {
            ReportId = report.ReportId,
            Title = report.Title,
            Content = report.Content,
            Type = report.Type,
            SendBy = report.SendBy,
            SendByUserName = user?.FullName ?? "Unknown",
            SendAt = report.SendAt,
            ReplyBy = report.ReplyBy,
            ReplyByUserName = null,
            ReplyAt = report.ReplyAt,
            ReplyContent = report.ReplyContent
        };
    }

    public async Task<ReportResponseDTO?> GetReportByIdAsync(int id, int? currentUserId, int? currentUserRoleId)
    {
        var report = await _reportRepository.FindByIdAsync(id);
        if (report == null)
        {
            return null;
        }

        // Check permissions
        if (!CanViewReport(report, currentUserId, currentUserRoleId))
        {
            throw new UnauthorizedAccessException("You do not have permission to view this report.");
        }

        // Get ReplyBy user if exists
        string? replyByUserName = null;
        if (report.ReplyBy.HasValue)
        {
            var replyUser = await _reportRepository.GetUserByIdAsync(report.ReplyBy.Value);
            replyByUserName = replyUser?.FullName;
        }

        return new ReportResponseDTO
        {
            ReportId = report.ReportId,
            Title = report.Title,
            Content = report.Content,
            Type = report.Type,
            SendBy = report.SendBy,
            SendByUserName = report.SendByNavigation?.FullName ?? "Unknown",
            SendAt = report.SendAt,
            ReplyBy = report.ReplyBy,
            ReplyByUserName = replyByUserName,
            ReplyAt = report.ReplyAt,
            ReplyContent = report.ReplyContent
        };
    }

    public async Task<PaginatedResultDTO<ReportResponseDTO>> GetReportsAsync(
        ReportQueryParams query, 
        int? currentUserId, 
        int? currentUserRoleId)
    {
        if (!currentUserRoleId.HasValue)
        {
            // Not authenticated - no access
            return new PaginatedResultDTO<ReportResponseDTO>
            {
                Items = new List<ReportResponseDTO>(),
                Total = 0,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalPages = 0,
                HasNext = false,
                HasPrevious = false
            };
        }

        // Apply role-based filtering
        string? typeFilter = null;
        int? sendByFilter = null;
        bool excludeSystem = false;

        // Staff (RoleId = 3) and Manager (RoleId = 2) can view Article and Exam reports
        if (currentUserRoleId.Value == 3 || currentUserRoleId.Value == 2)
        {
            // If query.Type is specified and it's System, deny access
            if (query.Type != null && query.Type.Equals("System", StringComparison.OrdinalIgnoreCase))
            {
                return new PaginatedResultDTO<ReportResponseDTO>
                {
                    Items = new List<ReportResponseDTO>(),
                    Total = 0,
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TotalPages = 0,
                    HasNext = false,
                    HasPrevious = false
                };
            }
            // Filter out System reports - query for Article and Exam only
            if (string.IsNullOrWhiteSpace(query.Type))
            {
                excludeSystem = true;
            }
            else
            {
                typeFilter = query.Type;
            }
        }
        // Admin (RoleId = 1) can view System reports only
        else if (currentUserRoleId.Value == 1)
        {
            // If query.Type is specified and it's not System, deny access
            if (query.Type != null && !query.Type.Equals("System", StringComparison.OrdinalIgnoreCase))
            {
                return new PaginatedResultDTO<ReportResponseDTO>
                {
                    Items = new List<ReportResponseDTO>(),
                    Total = 0,
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TotalPages = 0,
                    HasNext = false,
                    HasPrevious = false
                };
            }
            // Filter to only System reports
            typeFilter = "System";
        }
        // Regular users (RoleId = 4) can only view their own reports
        else
        {
            if (currentUserId.HasValue)
            {
                sendByFilter = currentUserId.Value;
            }
            else
            {
                return new PaginatedResultDTO<ReportResponseDTO>
                {
                    Items = new List<ReportResponseDTO>(),
                    Total = 0,
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TotalPages = 0,
                    HasNext = false,
                    HasPrevious = false
                };
            }
        }

        var (items, total) = await _reportRepository.QueryAsync(
            query.Page,
            query.PageSize,
            query.Search,
            typeFilter ?? query.Type,
            query.ArticleId,
            query.ExamId,
            query.IsReplied,
            query.SortBy ?? "sendAt",
            query.SortDir ?? "desc",
            sendByFilter,
            excludeSystem
        );

        var filteredList = items.ToList();
        var filteredTotal = total;

        // Get ReplyBy user names
        var reportDtos = new List<ReportResponseDTO>();
        foreach (var report in filteredList)
        {
            string? replyByUserName = null;
            if (report.ReplyBy.HasValue)
            {
                var replyUser = await _reportRepository.GetUserByIdAsync(report.ReplyBy.Value);
                replyByUserName = replyUser?.FullName;
            }

            reportDtos.Add(new ReportResponseDTO
            {
                ReportId = report.ReportId,
                Title = report.Title,
                Content = report.Content,
                Type = report.Type,
                SendBy = report.SendBy,
                SendByUserName = report.SendByNavigation?.FullName ?? "Unknown",
                SendAt = report.SendAt,
                ReplyBy = report.ReplyBy,
                ReplyByUserName = replyByUserName,
                ReplyAt = report.ReplyAt,
                ReplyContent = report.ReplyContent
            });
        }

        var totalPages = (int)Math.Ceiling((double)filteredTotal / query.PageSize);

        return new PaginatedResultDTO<ReportResponseDTO>
        {
            Items = reportDtos,
            Total = filteredTotal,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = totalPages,
            HasNext = query.Page < totalPages,
            HasPrevious = query.Page > 1
        };
    }

    public async Task<ReportResponseDTO?> ReplyToReportAsync(
        int reportId, 
        ReportReplyRequestDTO request, 
        int userId, 
        int userRoleId)
    {
        // Only Admin (1) and Manager (2) can reply
        if (userRoleId != 1 && userRoleId != 2)
        {
            throw new UnauthorizedAccessException("Only Admin and Manager can reply to reports.");
        }

        var report = await _reportRepository.FindByIdAsync(reportId);
        if (report == null)
        {
            return null;
        }

        // Check if report can be replied to based on role
        if (userRoleId == 2) // Manager
        {
            // Manager can only reply to Article and Exam reports
            if (report.Type == "System")
            {
                throw new UnauthorizedAccessException("Manager can only reply to Article and Exam reports.");
            }
        }
        // Admin can reply to all reports

        report.ReplyBy = userId;
        report.ReplyAt = DateTime.UtcNow;
        report.ReplyContent = request.ReplyContent;

        await _reportRepository.UpdateAsync(report);

        // Get ReplyBy user info
        var replyUser = await _reportRepository.GetUserByIdAsync(userId);

        _logger.LogInformation("Report {ReportId} replied by User {UserId}", reportId, userId);

        return new ReportResponseDTO
        {
            ReportId = report.ReportId,
            Title = report.Title,
            Content = report.Content,
            Type = report.Type,
            SendBy = report.SendBy,
            SendByUserName = report.SendByNavigation?.FullName ?? "Unknown",
            SendAt = report.SendAt,
            ReplyBy = report.ReplyBy,
            ReplyByUserName = replyUser?.FullName ?? "Unknown",
            ReplyAt = report.ReplyAt,
            ReplyContent = report.ReplyContent
        };
    }

    private bool CanViewReport(Report report, int? currentUserId, int? currentUserRoleId)
    {
        if (!currentUserRoleId.HasValue)
        {
            return false; // Not authenticated
        }

        // Admin can view System reports
        if (currentUserRoleId.Value == 1)
        {
            return report.Type == "System";
        }

        // Staff and Manager can view Article and Exam reports
        if (currentUserRoleId.Value == 2 || currentUserRoleId.Value == 3)
        {
            return report.Type != "System" && (report.Type.StartsWith("Article-") || report.Type.StartsWith("Exam-"));
        }

        // Regular users can only view their own reports
        if (currentUserId.HasValue)
        {
            return report.SendBy == currentUserId.Value;
        }

        return false;
    }
}

