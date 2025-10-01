using DataLayer.DTOs.Article;

namespace ServiceLayer.Article;

public interface IArticleService
{
    Task<ArticleResponseDTO> CreateArticleAsync(ArticleCreateDTO request, int creatorUserId);
}