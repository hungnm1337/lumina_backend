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
}