using DataLayer.Models;

namespace RepositoryLayer;

public interface IArticleRepository
{
    Task AddAsync(Article article);
    Task AddSectionsRangeAsync(IEnumerable<ArticleSection> sections);
    Task<List<Article>> GetAllWithCategoryAndUserAsync();
    Task<Article?> FindByIdAsync(int id);
    Task DeleteAsync(int id);
    Task UpdateAsync(Article article);
    Task<(List<Article> Items, int Total)> QueryAsync(int page, int pageSize, string? sortBy, string? sortDir, string? search, int? categoryId, bool? isPublished);
}