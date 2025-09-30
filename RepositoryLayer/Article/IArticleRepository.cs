using DataLayer.Models;

namespace RepositoryLayer;

public interface IArticleRepository
{
    Task AddAsync(Article article);
    Task AddSectionsRangeAsync(IEnumerable<ArticleSection> sections);
}