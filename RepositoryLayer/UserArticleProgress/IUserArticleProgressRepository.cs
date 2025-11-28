using DataLayer.Models;
using UserArticleProgressModel = DataLayer.Models.UserArticleProgress;

namespace RepositoryLayer.UserArticleProgress;

public interface IUserArticleProgressRepository
{
    Task<UserArticleProgressModel?> GetUserArticleProgressAsync(int userId, int articleId);
    Task<UserArticleProgressModel> SaveOrUpdateProgressAsync(int userId, int articleId, int progressPercent, string status);
    Task<List<UserArticleProgressModel>> GetUserArticleProgressesAsync(int userId, List<int> articleIds);
    Task MarkArticleAsDoneAsync(int userId, int articleId);
}

