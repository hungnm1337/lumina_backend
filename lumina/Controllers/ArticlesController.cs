using DataLayer.DTOs.Article;
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
    public IActionResult GetArticleById(int id)
    {
        // TODO: Implement this endpoint properly later.
        return Ok(new { Message = $"This is a placeholder for getting article with ID {id}." });
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories()
    {
        var list = await _unitOfWork.Categories.GetAllAsync();
        return Ok(list.Select(c => new { id = c.CategoryId, name = c.CategoryName }));
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ArticleResponseDTO>>> GetAllArticles()
    {
        try
        {
            var articles = await _articleService.GetAllArticlesAsync();
            return Ok(articles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving articles.");
            return StatusCode(500, new ErrorResponse("An internal server error occurred. Please try again later."));
        }
    }

    // GET api/articles/query?page=1&pageSize=10&sortBy=createdAt&sortDir=desc&search=...&categoryId=...&isPublished=true
    [HttpGet("query")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponse<ArticleResponseDTO>>> Query([FromQuery] ArticleQueryParams query)
    {
        try
        {
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

        var updated = await _articleService.UpdateArticleAsync(id, request, updaterUserId);
        if (updated == null)
        {
            return NotFound(new ErrorResponse($"Article with ID {id} not found."));
        }
        return Ok(updated);
    }

    //// POST api/articles/{id}/publish
    //[HttpPost("{id}/publish")]
    //[Authorize(Roles = "Staff")]
    //public async Task<ActionResult> Publish(int id, [FromBody] ArticlePublishRequest req)
    //{
    //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
    //    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var updaterUserId))
    //    {
    //        return Unauthorized(new ErrorResponse("Invalid token - User ID could not be determined."));
    //    }

    //    var ok = await _articleService.PublishArticleAsync(id, req.Publish, updaterUserId);
    //    if (!ok)
    //    {
    //        return NotFound(new ErrorResponse($"Article with ID {id} not found."));
    //    }
    //    return NoContent();
    //}
    [HttpPost("{id}/request-approval")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> RequestApproval(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var staffUserId))
        {
            return Unauthorized(new ErrorResponse("Invalid token."));
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