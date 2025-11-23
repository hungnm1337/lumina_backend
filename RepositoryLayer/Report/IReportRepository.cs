using DataLayer.Models;

namespace RepositoryLayer.Report;

public interface IReportRepository
{
    Task AddAsync(Report report);
    Task<Report?> FindByIdAsync(int id);
    Task<List<Report>> GetAllAsync();
    Task<Report?> UpdateAsync(Report report);
    Task<User?> GetUserByIdAsync(int userId);
    Task<(List<Report> items, int total)> QueryAsync(
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
        bool excludeSystem = false);
}

