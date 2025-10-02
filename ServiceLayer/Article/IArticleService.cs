using DataLayer.DTOs.Article;

namespace ServiceLayer.Article;

public interface IArticleService
{
    Task<ArticleResponseDTO> CreateArticleAsync(ArticleCreateDTO request, int creatorUserId);
    Task<List<ArticleResponseDTO>> GetAllArticlesAsync();
    Task<bool> DeleteArticleAsync(int id);
    Task<ArticleResponseDTO?> UpdateArticleAsync(int id, ArticleUpdateDTO request, int updaterUserId);
    Task<bool> PublishArticleAsync(int id, bool publish, int updaterUserId);
    Task<PagedResponse<ArticleResponseDTO>> QueryAsync(ArticleQueryParams query);
}