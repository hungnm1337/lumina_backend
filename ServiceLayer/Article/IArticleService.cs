
using DataLayer.DTOs.Article; 

public interface IArticleService
{
    Task<ArticleResponseDTO> CreateArticleAsync(ArticleCreateDTO request, int creatorUserId);
    Task<List<ArticleResponseDTO>> GetAllArticlesAsync();
    Task<bool> DeleteArticleAsync(int id);
    Task<ArticleResponseDTO?> UpdateArticleAsync(int id, ArticleUpdateDTO request, int updaterUserId);
    Task<bool> RequestApprovalAsync(int id, int staffUserId); 
    Task<bool> ReviewArticleAsync(int id, ArticleReviewRequest request, int managerUserId); 

    Task<PagedResponse<ArticleResponseDTO>> QueryAsync(ArticleQueryParams query);
}