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
}