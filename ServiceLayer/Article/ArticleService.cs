using DataLayer.Models;
using Microsoft.Extensions.Logging;
using RepositoryLayer.UnitOfWork;
using DataLayer.DTOs;
using DataLayer.DTOs.Article;
namespace ServiceLayer.Article;

public class ArticleService : IArticleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ArticleService> _logger;

    public ArticleService(IUnitOfWork unitOfWork, ILogger<ArticleService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ArticleResponseDTO> CreateArticleAsync(ArticleCreateDTO request, int creatorUserId)
    {
        var category = await _unitOfWork.Categories.FindByIdAsync(request.CategoryId);
        if (category == null)
        {
            throw new KeyNotFoundException("Category not found.");
        }

        var creator = await _unitOfWork.Users.GetUserByIdAsync(creatorUserId);
        if (creator == null)
        {
            throw new KeyNotFoundException("Creator user not found.");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var article = new DataLayer.Models.Article // Namespace đầy đủ để tránh nhầm lẫn
            {
                Title = request.Title,
                Summary = request.Summary,
                CategoryId = request.CategoryId,
                CreatedBy = creatorUserId,
                CreatedAt = DateTime.UtcNow,
                IsPublished = request.PublishNow,
                Status = request.PublishNow ? "Published" : "Draft"
            };

            await _unitOfWork.Articles.AddAsync(article);
            await _unitOfWork.CompleteAsync(); // Lưu để EF Core gán ArticleId cho object `article`

            var articleSections = new List<ArticleSection>();
            if (request.Sections != null && request.Sections.Any())
            {
                articleSections = request.Sections.Select(s => new ArticleSection
                {
                    ArticleId = article.ArticleId,
                    SectionTitle = s.SectionTitle,
                    SectionContent = s.SectionContent,
                    OrderIndex = s.OrderIndex
                }).ToList();

                await _unitOfWork.Articles.AddSectionsRangeAsync(articleSections);
                await _unitOfWork.CompleteAsync();
            }

            await transaction.CommitAsync();

            _logger.LogInformation("Successfully created article with ID {ArticleId} by User {UserId}", article.ArticleId, creatorUserId);

            return new ArticleResponseDTO
            {
                ArticleId = article.ArticleId,
                Title = article.Title,
                Summary = article.Summary,
                IsPublished = article.IsPublished,
                Status = article.Status,
                CreatedAt = article.CreatedAt,
                AuthorName = creator.FullName,
                CategoryName = category.CategoryName,
                RejectionReason = article.RejectionReason,
                Sections = articleSections.Select(s => new ArticleSectionResponseDTO
                {
                    SectionId = s.SectionId,
                    SectionTitle = s.SectionTitle,
                    SectionContent = s.SectionContent,
                    OrderIndex = s.OrderIndex
                }).OrderBy(s => s.OrderIndex).ToList()
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create article for user {UserId}", creatorUserId);
            throw;
        }
    }

    public async Task<List<ArticleResponseDTO>> GetAllArticlesAsync()
    {
        var articles = await _unitOfWork.Articles.GetAllWithCategoryAndUserAsync();
        
        return articles.Select(article => new ArticleResponseDTO
        {
            ArticleId = article.ArticleId,
            Title = article.Title,
            Summary = article.Summary,
            IsPublished = article.IsPublished,
            Status = article.Status,
            CreatedAt = article.CreatedAt,
            AuthorName = article.CreatedByNavigation?.FullName ?? "Unknown",
            CategoryName = article.Category?.CategoryName ?? "Unknown",
            RejectionReason = article.RejectionReason,
            Sections = article.ArticleSections?.Select(s => new ArticleSectionResponseDTO
            {
                SectionId = s.SectionId,
                SectionTitle = s.SectionTitle,
                SectionContent = s.SectionContent,
                OrderIndex = s.OrderIndex
            }).OrderBy(s => s.OrderIndex).ToList() ?? new List<ArticleSectionResponseDTO>()
        }).ToList();
    }

    public async Task<bool> DeleteArticleAsync(int id)
    {
        var article = await _unitOfWork.Articles.FindByIdAsync(id);
        if (article == null)
        {
            return false;
        }

        try
        {
            await _unitOfWork.Articles.DeleteAsync(id);
            _logger.LogInformation("Successfully deleted article with ID {ArticleId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete article with ID {ArticleId}", id);
            return false;
        }
    }

    public async Task<ArticleResponseDTO?> UpdateArticleAsync(int id, ArticleUpdateDTO request, int updaterUserId)
    {
        var article = await _unitOfWork.Articles.FindByIdAsync(id);
        if (article == null)
        {
            return null;
        }

        article.Title = request.Title;
        article.Summary = request.Summary;
        article.CategoryId = request.CategoryId;
        article.UpdatedBy = updaterUserId;
        article.UpdatedAt = DateTime.UtcNow;

        // Cập nhật metadata bài viết
        await _unitOfWork.Articles.UpdateAsync(article);

        // Cập nhật sections nếu có
        if (request.Sections != null && request.Sections.Any())
        {
            var newSections = request.Sections.Select(s => new ArticleSection
            {
                SectionTitle = s.SectionTitle,
                SectionContent = s.SectionContent,
                OrderIndex = s.OrderIndex
            }).ToList();

            await _unitOfWork.Articles.UpdateSectionsAsync(id, newSections);
        }

        // Lấy lại article với sections đã cập nhật
        article = await _unitOfWork.Articles.FindByIdAsync(id);
        var category = await _unitOfWork.Categories.FindByIdAsync(article.CategoryId);
        var author = await _unitOfWork.Users.GetUserByIdAsync(article.CreatedBy);

        return new ArticleResponseDTO
        {
            ArticleId = article.ArticleId,
            Title = article.Title,
            Summary = article.Summary,
            IsPublished = article.IsPublished,
            Status = article.Status,
            CreatedAt = article.CreatedAt,
            AuthorName = author?.FullName ?? "Unknown",
            CategoryName = category?.CategoryName ?? "Unknown",
            Sections = article.ArticleSections.OrderBy(s => s.OrderIndex).Select(s => new ArticleSectionResponseDTO
            {
                SectionId = s.SectionId,
                SectionTitle = s.SectionTitle,
                SectionContent = s.SectionContent,
                OrderIndex = s.OrderIndex
            }).ToList()
        };
    }

    public async Task<bool> RequestApprovalAsync(int id, int staffUserId)
    {
        var article = await _unitOfWork.Articles.FindByIdAsync(id);
        if (article == null || (article.Status != "Draft" && article.Status != "Rejected"))
        {
            // Chỉ cho phép gửi duyệt bài viết đang là bản nháp hoặc đã bị từ chối
            return false;
        }

        article.Status = "Pending";
        article.IsPublished = false; // Vẫn là chưa xuất bản
        article.UpdatedBy = staffUserId;
        article.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Articles.UpdateAsync(article);
        return true;
    }

    public async Task<PagedResponse<ArticleResponseDTO>> QueryAsync(ArticleQueryParams query)
    {
        var (items, total) = await _unitOfWork.Articles.QueryAsync(
        query.Page, query.PageSize, query.Search, query.CategoryId, query.IsPublished, query.Status, query.SortBy, query.SortDir, query.CreatedBy
    );

        var mapped = items.Select(a => new ArticleResponseDTO
        {
            ArticleId = a.ArticleId,
            Title = a.Title,
            Summary = a.Summary,
            IsPublished = a.IsPublished,
            Status = a.Status,
            CreatedAt = a.CreatedAt,
            AuthorName = a.CreatedByNavigation?.FullName ?? "Unknown",
            CategoryName = a.Category?.CategoryName ?? "Unknown",
            RejectionReason = a.RejectionReason,
            Sections = a.ArticleSections?.OrderBy(s => s.OrderIndex).Select(s => new ArticleSectionResponseDTO
            {
                SectionId = s.SectionId,
                SectionTitle = s.SectionTitle,
                SectionContent = s.SectionContent,
                OrderIndex = s.OrderIndex
            }).ToList() ?? new List<ArticleSectionResponseDTO>()
        }).ToList();

        return new PagedResponse<ArticleResponseDTO>
        {
            Items = mapped,
            Total = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
    public async Task<bool> ReviewArticleAsync(int id, ArticleReviewRequest request, int managerUserId)
    {
        var article = await _unitOfWork.Articles.FindByIdAsync(id);
        if (article == null || article.Status != "Pending")
        {
            // Chỉ duyệt được bài đang chờ
            return false;
        }

        if (request.IsApproved)
        {
            article.Status = "Published";
            article.IsPublished = true;
            article.RejectionReason = null; // Clear rejection reason when approved
        }
        else
        {
            article.Status = "Rejected"; // Changed from Draft to Rejected
            article.IsPublished = false;
            article.RejectionReason = request.Comment; // Save rejection reason
        }

        article.UpdatedBy = managerUserId;
        article.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Articles.UpdateAsync(article);
        return true;
    }
    public async Task<ArticleResponseDTO?> GetArticleByIdAsync(int id)
    {
        var article = await _unitOfWork.Articles.FindByIdAsync(id);
        if (article == null || article.IsPublished != true) // Chỉ lấy bài đã published
        {
            return null;
        }

        var category = await _unitOfWork.Categories.FindByIdAsync(article.CategoryId);
        var author = await _unitOfWork.Users.GetUserByIdAsync(article.CreatedBy);

        return new ArticleResponseDTO
        {
            ArticleId = article.ArticleId,
            Title = article.Title,
            Summary = article.Summary,
            IsPublished = article.IsPublished,
            Status = article.Status,
            CreatedAt = article.CreatedAt,
            AuthorName = author?.FullName ?? "Unknown",
            CategoryName = category?.CategoryName ?? "Unknown",
            RejectionReason = article.RejectionReason,
            Sections = article.ArticleSections
                .OrderBy(s => s.OrderIndex)
                .Select(s => new ArticleSectionResponseDTO
                {
                    SectionId = s.SectionId,
                    SectionTitle = s.SectionTitle,
                    SectionContent = s.SectionContent,
                    OrderIndex = s.OrderIndex
                }).ToList()
        };
    }
}