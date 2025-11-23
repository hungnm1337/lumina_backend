using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace RepositoryLayer.Report;

public class ReportRepository : IReportRepository
{
    private readonly LuminaSystemContext _context;

    public ReportRepository(LuminaSystemContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Report report)
    {
        await _context.Reports.AddAsync(report);
    }

    public async Task<Report?> FindByIdAsync(int id)
    {
        return await _context.Reports
            .Include(r => r.SendByNavigation)
            .FirstOrDefaultAsync(r => r.ReportId == id);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<List<Report>> GetAllAsync()
    {
        return await _context.Reports
            .Include(r => r.SendByNavigation)
            .OrderByDescending(r => r.SendAt)
            .ToListAsync();
    }

    public async Task<Report?> UpdateAsync(Report report)
    {
        _context.Reports.Update(report);
        await _context.SaveChangesAsync();
        return report;
    }

    public async Task<(List<Report> items, int total)> QueryAsync(
        int page, 
        int pageSize, 
        string? search, 
        string? type, 
        int? articleId, 
        int? examId, 
        bool? isReplied, 
        string sortBy, 
        string sortDir,
        int? sendBy = null,
        bool excludeSystem = false)
    {
        var query = _context.Reports
            .Include(r => r.SendByNavigation)
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(r => 
                r.Title.ToLower().Contains(s) || 
                r.Content.ToLower().Contains(s));
        }

        // Type filter
        if (!string.IsNullOrWhiteSpace(type))
        {
            if (type.Equals("System", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => r.Type == "System");
            }
            else if (type.Equals("Article", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => r.Type.StartsWith("Article-"));
            }
            else if (type.Equals("Exam", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => r.Type.StartsWith("Exam-"));
            }
        }

        // Exclude System filter (for Staff/Manager)
        if (excludeSystem)
        {
            query = query.Where(r => r.Type != "System" && (r.Type.StartsWith("Article-") || r.Type.StartsWith("Exam-")));
        }

        // ArticleId filter
        if (articleId.HasValue)
        {
            var articleType = $"Article-{articleId.Value}";
            query = query.Where(r => r.Type == articleType);
        }

        // ExamId filter
        if (examId.HasValue)
        {
            var examType = $"Exam-{examId.Value}";
            query = query.Where(r => r.Type == examType);
        }

        // IsReplied filter
        if (isReplied.HasValue)
        {
            if (isReplied.Value)
            {
                query = query.Where(r => r.ReplyBy != null && r.ReplyAt != null);
            }
            else
            {
                query = query.Where(r => r.ReplyBy == null || r.ReplyAt == null);
            }
        }

        // SendBy filter (for regular users to see only their own reports)
        if (sendBy.HasValue)
        {
            query = query.Where(r => r.SendBy == sendBy.Value);
        }

        // Sorting
        bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy?.ToLower()) switch
        {
            "title" => desc ? query.OrderByDescending(r => r.Title) : query.OrderBy(r => r.Title),
            _ => desc ? query.OrderByDescending(r => r.SendAt) : query.OrderBy(r => r.SendAt)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }
}

