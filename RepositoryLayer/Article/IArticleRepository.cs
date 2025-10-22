using DataLayer.Models;

namespace RepositoryLayer;

public interface IArticleRepository
{
    Task AddAsync(Article article);
    Task AddSectionsRangeAsync(IEnumerable<ArticleSection> sections);
    Task<List<Article>> GetAllWithCategoryAndUserAsync();
    Task<Article?> FindByIdAsync(int id);
    Task<bool> DeleteAsync(int id);
    Task<Article?> UpdateAsync(Article article);
    Task UpdateSectionsAsync(int articleId, IEnumerable<ArticleSection> newSections);
    Task<(List<Article> items, int total)> QueryAsync(int page, int pageSize, string? search, int? categoryId, bool? isPublished, string? status, string sortBy, string sortDir, int? createdBy = null);
}