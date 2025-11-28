using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using UserArticleProgressModel = DataLayer.Models.UserArticleProgress;

namespace RepositoryLayer.UserArticleProgress;

public class UserArticleProgressRepository : IUserArticleProgressRepository
{
    private readonly LuminaSystemContext _context;

    public UserArticleProgressRepository(LuminaSystemContext context)
    {
        _context = context;
    }

    public async Task<UserArticleProgressModel?> GetUserArticleProgressAsync(int userId, int articleId)
    {
        return await _context.UserArticleProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ArticleId == articleId);
    }

    public async Task<UserArticleProgressModel> SaveOrUpdateProgressAsync(int userId, int articleId, int progressPercent, string status)
    {
        var existingProgress = await GetUserArticleProgressAsync(userId, articleId);

        if (existingProgress != null)
        {
            // Update existing progress
            existingProgress.ProgressPercent = progressPercent;
            existingProgress.Status = status;
            existingProgress.LastAccessedAt = DateTime.UtcNow;
            
            if (status == "completed" && existingProgress.CompletedAt == null)
            {
                existingProgress.CompletedAt = DateTime.UtcNow;
            }
            else if (status != "completed")
            {
                existingProgress.CompletedAt = null;
            }

            _context.UserArticleProgresses.Update(existingProgress);
            await _context.SaveChangesAsync();
            return existingProgress;
        }
        else
        {
            // Create new progress
            var newProgress = new UserArticleProgressModel
            {
                UserId = userId,
                ArticleId = articleId,
                ProgressPercent = progressPercent,
                Status = status,
                LastAccessedAt = DateTime.UtcNow,
                CompletedAt = status == "completed" ? DateTime.UtcNow : null
            };

            await _context.UserArticleProgresses.AddAsync(newProgress);
            await _context.SaveChangesAsync();
            return newProgress;
        }
    }

    public async Task<List<UserArticleProgressModel>> GetUserArticleProgressesAsync(int userId, List<int> articleIds)
    {
        return await _context.UserArticleProgresses
            .Where(p => p.UserId == userId && articleIds.Contains(p.ArticleId))
            .ToListAsync();
    }

    public async Task MarkArticleAsDoneAsync(int userId, int articleId)
    {
        await SaveOrUpdateProgressAsync(userId, articleId, 100, "completed");
    }
}

