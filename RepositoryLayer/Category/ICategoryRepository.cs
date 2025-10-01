using DataLayer.Models;

namespace RepositoryLayer;

public interface ICategoryRepository
{
    Task<ArticleCategory?> FindByIdAsync(int id);
    Task<List<ArticleCategory>> GetAllAsync();
}