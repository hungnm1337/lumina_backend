using DataLayer.DTOs.UserReport;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RepositoryLayer.Report;

public class ReportRepository : IReportRepository
{
    private readonly LuminaSystemContext _context;

    public ReportRepository(LuminaSystemContext context)
    {
        _context = context;
    }

    public async Task<bool> AddAsync(UserReportRequest report)
    {
        DataLayer.Models.Report newReport = new DataLayer.Models.Report
        {
            Title = report.Title,
            Content = report.Content,
            SendBy = report.SendBy,
            SendAt = DateTime.UtcNow, // Ensure a date is set
            ReplyBy = report.ReplyBy,
            ReplyAt = report.ReplyAt,
            ReplyContent = report.ReplyContent,
            Type = report.Type
        };

        await _context.Reports.AddAsync(newReport);
        int created = await _context.SaveChangesAsync();
        return created > 0;
    }
    
    public async Task<UserReportResponse?> FindByIdAsync(int id)
    {
        // Note: When using .Select(), .Include() is not strictly necessary 
        // as EF Core infers the joins from the projection.
        var report = await _context.Reports
            .Where(r => r.ReportId == id)
            .Select(r => new UserReportResponse
            {
                ReportId = r.ReportId,
                Title = r.Title,
                Content = r.Content,
                SendBy = r.SendByNavigation.FullName,
                SendAt = r.SendAt,
                // Subquery to fetch ReplyBy name safely
                ReplyBy = r.ReplyBy.HasValue
                    ? _context.Users
                        .Where(u => u.UserId == r.ReplyBy.Value)
                        .Select(u => u.FullName)
                        .FirstOrDefault()
                    : null,
                ReplyAt = r.ReplyAt,
                ReplyContent = r.ReplyContent,
                Type = r.Type
            })
            .FirstOrDefaultAsync();

        return report;
    }

    public async Task<List<UserReportResponse>> GetAllAsync(int roleid)
    {
        // 1. Start the query but do not execute it yet
        var query = _context.Reports.AsQueryable();

        // 2. Apply filtering logic BEFORE fetching data (Server-side evaluation)
        // Admin (1) - System reports
        if (roleid == 1)
        {
            query = query.Where(r => r.Type == "System");
        }
        // Manager (2, 3) - Staff exams, articles reports
        else if (roleid == 2 || roleid == 3)
        {
            query = query.Where(r => r.Type != null &&
                                    (r.Type.Contains("Article") || r.Type.Contains("Exam")));
        }

        // 3. Execute the query with projection
        return await query
            .Select(r => new UserReportResponse
            {
                ReportId = r.ReportId,
                Title = r.Title,
                Content = r.Content,
                SendBy = r.SendByNavigation.FullName,
                SendAt = r.SendAt,
                ReplyBy = r.ReplyBy.HasValue
                    ? _context.Users
                        .Where(u => u.UserId == r.ReplyBy.Value)
                        .Select(u => u.FullName)
                        .FirstOrDefault()
                    : null,
                ReplyAt = r.ReplyAt,
                ReplyContent = r.ReplyContent,
                Type = r.Type
            })
            .ToListAsync();
    }

    public async Task<List<UserReportResponse>> GetAllByUserIdAsync(int userId)
    {
        return await _context.Reports
            .Where(r => r.SendBy == userId)
            .Select(r => new UserReportResponse
            {
                ReportId = r.ReportId,
                Title = r.Title,
                Content = r.Content,
                SendBy = r.SendByNavigation.FullName,
                SendAt = r.SendAt,
                ReplyBy = r.ReplyBy.HasValue
                    ? _context.Users
                        .Where(u => u.UserId == r.ReplyBy.Value)
                        .Select(u => u.FullName)
                        .FirstOrDefault()
                    : null,
                ReplyAt = r.ReplyAt,
                ReplyContent = r.ReplyContent,
                Type = r.Type
            })
            .ToListAsync();
    }

    public async Task<bool> UpdateAsync(UserReportRequest report)
    {
        var existingReport = await _context.Reports.FindAsync(report.ReportId);

        if (existingReport == null)
        {
            return false;
        }

        existingReport.ReplyBy = report.ReplyBy;
        existingReport.ReplyAt = DateTime.UtcNow; // Set reply time to now
        existingReport.ReplyContent = report.ReplyContent;

        _context.Reports.Update(existingReport);
        int updated = await _context.SaveChangesAsync();
        return updated > 0;
    }
}