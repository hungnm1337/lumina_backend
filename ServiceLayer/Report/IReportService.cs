using DataLayer.DTOs;
using DataLayer.DTOs.Report;

namespace ServiceLayer.Report;

public interface IReportService
{
    Task<ReportResponseDTO> CreateReportAsync(ReportCreateRequestDTO request, int userId);
    Task<ReportResponseDTO?> GetReportByIdAsync(int id, int? currentUserId, int? currentUserRoleId);
    Task<PaginatedResultDTO<ReportResponseDTO>> GetReportsAsync(ReportQueryParams query, int? currentUserId, int? currentUserRoleId);
    Task<ReportResponseDTO?> ReplyToReportAsync(int reportId, ReportReplyRequestDTO request, int userId, int userRoleId);
}

