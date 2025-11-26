using DataLayer.Models;

namespace RepositoryLayer;

public interface ICategoryRepository
{
    Task<ArticleCategory?> FindByIdAsync(int id);
    Task<List<ArticleCategory>> GetAllAsync();
    Task<ArticleCategory> AddAsync(ArticleCategory category);
    Task<bool> ExistsByNameAsync(string categoryName);
}