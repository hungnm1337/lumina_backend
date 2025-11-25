using DataLayer.DTOs.UserReport;
using Microsoft.Extensions.Logging;
using RepositoryLayer.Report;

namespace ServiceLayer.Report;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IReportRepository reportRepository,
        ILogger<ReportService> logger)
    {
        _reportRepository = reportRepository;
        _logger = logger;
    }

    public async Task<bool> AddAsync(UserReportRequest report)
    {
        try
        {
            _logger.LogInformation("Creating report: {Title} by User {SendBy}", report.Title, report.SendBy);
            
            var result = await _reportRepository.AddAsync(report);
            
            if (result)
            {
                _logger.LogInformation("Report created successfully by User {SendBy}", report.SendBy);
            }
            else
            {
                _logger.LogWarning("Failed to create report by User {SendBy}", report.SendBy);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating report by User {SendBy}", report.SendBy);
            throw;
        }
    }

    public async Task<UserReportResponse> FindByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("Finding report by ID: {ReportId}", id);
            
            var report = await _reportRepository.FindByIdAsync(id);
            
            if (report == null)
            {
                _logger.LogWarning("Report not found: {ReportId}", id);
            }
            
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding report by ID: {ReportId}", id);
            throw;
        }
    }

    public async Task<List<UserReportResponse>> GetAllAsync(int roleid)
    {
        try
        {
            _logger.LogInformation("Getting all reports for role: {RoleId}", roleid);
            
            var reports = await _reportRepository.GetAllAsync(roleid);
            
            _logger.LogInformation("Found {Count} reports for role {RoleId}", reports.Count, roleid);
            
            return reports;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports for role: {RoleId}", roleid);
            throw;
        }
    }

    public async Task<List<UserReportResponse>> GetAllByUserIdAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Getting all reports for user: {UserId}", userId);
            
            var reports = await _reportRepository.GetAllByUserIdAsync(userId);
            
            _logger.LogInformation("Found {Count} reports for user {UserId}", reports.Count, userId);
            
            return reports;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(UserReportRequest report)
    {
        try
        {
            _logger.LogInformation("Updating report: {ReportId}", report.ReportId);
            
            var result = await _reportRepository.UpdateAsync(report);
            
            if (result)
            {
                _logger.LogInformation("Report {ReportId} updated successfully", report.ReportId);
            }
            else
            {
                _logger.LogWarning("Failed to update report: {ReportId}", report.ReportId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating report: {ReportId}", report.ReportId);
            throw;
        }
    }
}

