﻿using DataLayer.DTOs.Article;
using DataLayer.DTOs.Auth;
using Microsoft.AspNetCore.Authorization; // Vẫn giữ using này để dễ dàng bật lại
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Article;
using System.Security.Claims;

namespace lumina.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _articleService;
    private readonly ILogger<ArticlesController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public ArticlesController(IArticleService articleService, ILogger<ArticlesController> logger, IUnitOfWork unitOfWork)
    {
        _articleService = articleService;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    [HttpPost]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> CreateArticle([FromBody] ArticleCreateDTO request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var creatorUserId))
            {
                _logger.LogWarning("Invalid token: Claim 'NameIdentifier' not found or invalid.");
                return Unauthorized(new ErrorResponse("Invalid token - User ID could not be determined."));
            }

            var createdArticle = await _articleService.CreateArticleAsync(request, creatorUserId);

            return CreatedAtAction(nameof(GetArticleById), new { id = createdArticle.ArticleId }, createdArticle);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex.Message);
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while creating an article.");
            return StatusCode(500, new ErrorResponse("An internal server error occurred. Please try again later."));
        }
    }

    [HttpGet("{id}", Name = "GetArticleById")]
    [AllowAnonymous]
    public async Task<IActionResult> GetArticleById(int id)
    {
        var article = await _articleService.GetArticleByIdAsync(id);
        if (article == null)
        {
            return NotFound();
        }
        return Ok(article);
    }

    // API riêng cho việc xem bài viết công khai (chỉ bài đã published)
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ArticleResponseDTO>>> GetPublicArticles()
    {
        try
        {
            var query = new ArticleQueryParams
            {
                Page = 1,
                PageSize = 1000,
                IsPublished = true,
                Status = "Published"
            };
            var result = await _articleService.QueryAsync(query);
            return Ok(result.Items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving public articles.");
            return StatusCode(500, new ErrorResponse("An internal server error occurred. Please try again later."));
        }
    }

    // API debug để kiểm tra dữ liệu
    [HttpGet("debug")]
    [Authorize]
    public async Task<ActionResult> DebugArticles()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
            {
                return Unauthorized(new ErrorResponse("Invalid token - User ID could not be determined."));
            }

            var user = await _unitOfWork.Users.GetUserByIdAsync(currentUserId);
            if (user == null)
            {
                return Unauthorized(new ErrorResponse("User not found."));
            }

            // Lấy tất cả bài viết trong database
            var allArticles = await _unitOfWork.Articles.GetAllWithCategoryAndUserAsync();
            
            // Lấy bài viết của user hiện tại
            var userArticles = allArticles.Where(a => a.CreatedBy == currentUserId).ToList();

            // Test query với CreatedBy
            var query = new ArticleQueryParams
            {
                Page = 1,
                PageSize = 1000,
                CreatedBy = currentUserId
            };
            var queryResult = await _articleService.QueryAsync(query);

            return Ok(new
            {
                CurrentUserId = currentUserId,
                UserRoleId = user.RoleId,
                TotalArticlesInDB = allArticles.Count,
                UserArticlesCount = userArticles.Count,
                UserArticles = userArticles.Select(a => new
                {
                    ArticleId = a.ArticleId,
                    Title = a.Title,
                    CreatedBy = a.CreatedBy,
                    Status = a.Status,
                    IsPublished = a.IsPublished
                }),
                QueryResultCount = queryResult.Items.Count,
                QueryResult = queryResult.Items.Select(a => new
                {
                    ArticleId = a.ArticleId,
                    Title = a.Title,
                    AuthorName = a.AuthorName,
                    Status = a.Status,
                    IsPublished = a.IsPublished
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while debugging articles.");
            return StatusCode(500, new ErrorResponse("An internal server error occurred. Please try again later."));
        }
    }

    // API riêng cho staff để xem chi tiết bài viết của họ (bao gồm cả draft, pending, rejected)
    [HttpGet("my/{id}")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GetMyArticleById(int id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
            {
                return Unauthorized(new ErrorResponse("Invalid token - User ID could not be determined."));
            }

            // Lấy thông tin user để kiểm tra role
            var user = await _unitOfWork.Users.GetUserByIdAsync(currentUserId);
            if (user == null)
            {
                return Unauthorized(new ErrorResponse("User not found."));
            }

            // Nếu là Staff (RoleID = 3), chỉ có thể xem bài viết của chính họ
            if (user.RoleId == 3)
            {
                var article = await _unitOfWork.Articles.FindByIdAsync(id);
                if (article == null)
                {
                    return NotFound(new ErrorResponse($"Article with ID {id} not found."));
                }

                if (article.CreatedBy != currentUserId)
                {
                    return Forbid("You can only view your own articles.");
                }
            }

            // Lấy chi tiết bài viết (không cần kiểm tra IsPublished)
            var articleDetail = await _unitOfWork.Articles.FindByIdAsync(id);
            if (articleDetail == null)
            {
                return NotFound(new ErrorResponse($"Article with ID {id} not found."));
            }

            var category = await _unitOfWork.Categories.FindByIdAsync(articleDetail.CategoryId);
            var author = await _unitOfWork.Users.GetUserByIdAsync(articleDetail.CreatedBy);

            var response = new ArticleResponseDTO
            {
                ArticleId = articleDetail.ArticleId,
                Title = articleDetail.Title,
                Summary = articleDetail.Summary,
                IsPublished = articleDetail.IsPublished,
                Status = articleDetail.Status,
                CreatedAt = articleDetail.CreatedAt,
                AuthorName = author?.FullName ?? "Unknown",
                CategoryName = category?.CategoryName ?? "Unknown",
                RejectionReason = articleDetail.RejectionReason,
                Sections = articleDetail.ArticleSections
                    .OrderBy(s => s.OrderIndex)
                    .Select(s => new ArticleSectionResponseDTO
                    {
                        SectionId = s.SectionId,
                        SectionTitle = s.SectionTitle,
                        SectionContent = s.SectionContent,
                        OrderIndex = s.OrderIndex
                    }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving article with ID {ArticleId}.", id);
            return StatusCode(500, new ErrorResponse("An internal server error occurred. Please try again later."));
        }
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories()
    {
        var list = await _unitOfWork.Categories.GetAllAsync();
        return Ok(list.Select(c => new { id = c.CategoryId, name = c.CategoryName }));
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<ArticleResponseDTO>>> GetAllArticles()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
            {
                _logger.LogWarning("Invalid token: Claim 'NameIdentifier' not found or invalid.");
                return Unauthorized(new ErrorResponse("Invalid token - User ID could not be determined."));
            }

            _logger.LogInformation("Getting articles for user {UserId}", currentUserId);

            // Lấy role của user hiện tại
            var user = await _unitOfWork.Users.GetUserByIdAsync(currentUserId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found in database", currentUserId);
                return Unauthorized(new ErrorResponse("User not found."));
            }

            _logger.LogInformation("User {UserId} has RoleId {RoleId}", currentUserId, user.RoleId);

            // Nếu là Staff (RoleID = 3), chỉ lấy bài viết của chính họ
            if (user.RoleId == 3)
            {
                _logger.LogInformation("User {UserId} is Staff, filtering articles by CreatedBy", currentUserId);
                var query = new ArticleQueryParams
                {
                    Page = 1,
                    PageSize = 1000, // Lấy tất cả bài viết của staff
                    CreatedBy = currentUserId
                };
                var result = await _articleService.QueryAsync(query);
                _logger.LogInformation("Found {Count} articles for Staff {UserId}", result.Items.Count, currentUserId);
                return Ok(result.Items);
            }
            else
            {
                _logger.LogInformation("User {UserId} is not Staff (RoleId: {RoleId}), getting all articles", currentUserId, user.RoleId);
                // Manager và Admin có thể xem tất cả bài viết
                var articles = await _articleService.GetAllArticlesAsync();
                _logger.LogInformation("Found {Count} total articles", articles.Count);
                return Ok(articles);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving articles.");
            return StatusCode(500, new ErrorResponse("An internal server error occurred. Please try again later."));
        }
    }

    // GET api/articles/query?page=1&pageSize=10&sortBy=createdAt&sortDir=desc&search=...&categoryId=...&isPublished=true
    [HttpGet("query")]
    [Authorize]
    public async Task<ActionResult<PagedResponse<ArticleResponseDTO>>> Query([FromQuery] ArticleQueryParams query)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
            {
                return Unauthorized(new ErrorResponse("Invalid token - User ID could not be determined."));
            }

            // Lấy role của user hiện tại
            var user = await _unitOfWork.Users.GetUserByIdAsync(currentUserId);
            if (user == null)
            {
                return Unauthorized(new ErrorResponse("User not found."));
            }

            // Nếu là Staff (RoleID = 3), chỉ lấy bài viết của chính họ
            if (user.RoleId == 3)
            {
                query.CreatedBy = currentUserId;
            }
            // Manager và Admin có thể xem tất cả bài viết (không cần filter)

            var result = await _articleService.QueryAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while querying articles.");
            return StatusCode(500, new ErrorResponse("An internal server error occurred. Please try again later."));
        }
    }

    // PUT api/articles/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<ArticleResponseDTO>> Update(int id, [FromBody] ArticleUpdateDTO request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var updaterUserId))
        {
            return Unauthorized(new ErrorResponse("Invalid token - User ID could not be determined."));
        }

        // Kiểm tra xem staff có phải là tác giả của bài viết không
        var article = await _unitOfWork.Articles.FindByIdAsync(id);
        if (article == null)
        {
            return NotFound(new ErrorResponse($"Article with ID {id} not found."));
        }

        // Lấy thông tin user để kiểm tra role
        var user = await _unitOfWork.Users.GetUserByIdAsync(updaterUserId);
        if (user == null)
        {
            return Unauthorized(new ErrorResponse("User not found."));
        }

        // Nếu là Staff (RoleID = 3), chỉ có thể update bài viết của chính họ
        if (user.RoleId == 3 && article.CreatedBy != updaterUserId)
        {
            return Forbid("You can only update your own articles.");
        }

        var updated = await _articleService.UpdateArticleAsync(id, request, updaterUserId);
        if (updated == null)
        {
            return NotFound(new ErrorResponse($"Article with ID {id} not found."));
        }
        return Ok(updated);
    }

    [HttpPost("{id}/request-approval")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> RequestApproval(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var staffUserId))
        {
            return Unauthorized(new ErrorResponse("Invalid token."));
        }

        // Kiểm tra xem staff có phải là tác giả của bài viết không
        var article = await _unitOfWork.Articles.FindByIdAsync(id);
        if (article == null)
        {
            return NotFound(new ErrorResponse($"Article with ID {id} not found."));
        }

        // Lấy thông tin user để kiểm tra role
        var user = await _unitOfWork.Users.GetUserByIdAsync(staffUserId);
        if (user == null)
        {
            return Unauthorized(new ErrorResponse("User not found."));
        }

        // Nếu là Staff (RoleID = 3), chỉ có thể gửi duyệt bài viết của chính họ
        if (user.RoleId == 3 && article.CreatedBy != staffUserId)
        {
            return Forbid("You can only submit your own articles for approval.");
        }

        var ok = await _articleService.RequestApprovalAsync(id, staffUserId);
        if (!ok)
        {
            return NotFound(new ErrorResponse($"Article with ID {id} not found or cannot be submitted for approval."));
        }
        return NoContent();
    }

    [HttpPost("{id}/review")]
    [Authorize(Roles = "Manager")] // <-- Chỉ Manager mới có quyền này
    public async Task<IActionResult> ReviewArticle(int id, [FromBody] ArticleReviewRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var managerUserId))
        {
            return Unauthorized(new ErrorResponse("Invalid token."));
        }

        var ok = await _articleService.ReviewArticleAsync(id, request, managerUserId);
        if (!ok)
        {
            return NotFound(new ErrorResponse($"Article with ID {id} not found or is not pending review."));
        }
        return NoContent();
    }
    [HttpDelete("{id}")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult> DeleteArticle(int id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserId))
            {
                return Unauthorized(new ErrorResponse("Invalid token - User ID could not be determined."));
            }

            // Kiểm tra xem staff có phải là tác giả của bài viết không
            var article = await _unitOfWork.Articles.FindByIdAsync(id);
            if (article == null)
            {
                return NotFound(new ErrorResponse($"Article with ID {id} not found."));
            }

            // Lấy thông tin user để kiểm tra role
            var user = await _unitOfWork.Users.GetUserByIdAsync(currentUserId);
            if (user == null)
            {
                return Unauthorized(new ErrorResponse("User not found."));
            }

            // Nếu là Staff (RoleID = 3), chỉ có thể xóa bài viết của chính họ
            if (user.RoleId == 3 && article.CreatedBy != currentUserId)
            {
                return Forbid("You can only delete your own articles.");
            }

            var result = await _articleService.DeleteArticleAsync(id);
            if (!result)
            {
                return NotFound(new ErrorResponse($"Article with ID {id} not found."));
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting article with ID {ArticleId}.", id);
            return StatusCode(500, new ErrorResponse("An internal server error occurred. Please try again later."));
        }
    }
}