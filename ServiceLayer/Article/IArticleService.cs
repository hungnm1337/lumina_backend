
using DataLayer.DTOs.Article; 

public interface IArticleService
{
    Task<ArticleResponseDTO> CreateArticleAsync(ArticleCreateDTO request, int creatorUserId);
    Task<List<ArticleResponseDTO>> GetAllArticlesAsync();
    Task<bool> DeleteArticleAsync(int id);
    Task<ArticleResponseDTO?> UpdateArticleAsync(int id, ArticleUpdateDTO request, int updaterUserId);
    Task<bool> RequestApprovalAsync(int id, int staffUserId); 
    Task<bool> ReviewArticleAsync(int id, ArticleReviewRequest request, int managerUserId);
    Task<ArticleResponseDTO?> GetArticleByIdAsync(int id);
    Task<ArticleResponseDTO?> GetArticleByIdForManagerAsync(int id); // For manager to view any article including pending
    Task<PagedResponse<ArticleResponseDTO>> QueryAsync(ArticleQueryParams query);
    
    // Article Progress methods
    Task<ArticleProgressResponseDTO> SaveArticleProgressAsync(int userId, int articleId, ArticleProgressRequestDTO request);
    Task<List<ArticleProgressResponseDTO>> GetUserArticleProgressesAsync(int userId, List<int> articleIds);
    Task<bool> MarkArticleAsDoneAsync(int userId, int articleId);
    
    // Hide/Show article (Manager only) - Uses IsPublished field
    Task<bool> ToggleHideArticleAsync(int articleId, bool isPublished, int managerUserId);
}