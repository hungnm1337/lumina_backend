using DataLayer.DTOs.Article;
using DataLayer.DTOs.Auth;
using DataLayer.Models;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer;
using System.Security.Claims;
using ServiceLayer.Article;
using RepositoryLayer.User;

namespace Lumina.Tests;

public class ArticlesControllerTests
{
    private readonly Mock<IArticleService> _mockArticleService;
    private readonly Mock<ILogger<ArticlesController>> _mockLogger;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IArticleRepository> _mockArticleRepository;
    private readonly Mock<ICategoryRepository> _mockCategoryRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly ArticlesController _controller;

    public ArticlesControllerTests()
    {
        _mockArticleService = new Mock<IArticleService>();
        _mockLogger = new Mock<ILogger<ArticlesController>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockArticleRepository = new Mock<IArticleRepository>();
        _mockCategoryRepository = new Mock<ICategoryRepository>();
        _mockUserRepository = new Mock<IUserRepository>();

        _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);
        _mockUnitOfWork.Setup(u => u.Categories).Returns(_mockCategoryRepository.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

        _controller = new ArticlesController(_mockArticleService.Object, _mockLogger.Object, _mockUnitOfWork.Object);
    }

    #region CreateArticle Tests

    [Fact]
    public async Task CreateArticle_WithValidRequest_ShouldReturn201Created()
    {
        // Arrange
        var userId = 1;
        var request = CreateValidArticleCreateDTO();
        var expectedResponse = CreateArticleResponseDTO(1, userId);

        SetupUserClaims(userId, "Staff");
        _mockArticleService.Setup(s => s.CreateArticleAsync(It.IsAny<ArticleCreateDTO>(), userId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateArticle(request);

        // Assert
        var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdAtResult.StatusCode);
        var response = Assert.IsType<ArticleResponseDTO>(createdAtResult.Value);
        Assert.Equal(expectedResponse.ArticleId, response.ArticleId);
        _mockArticleService.Verify(s => s.CreateArticleAsync(It.Is<ArticleCreateDTO>(r =>
            r.Title == request.Title && r.CategoryId == request.CategoryId), userId), Times.Once);
    }

    [Fact]
    public async Task CreateArticle_WithInvalidModelState_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = CreateValidArticleCreateDTO();
        _controller.ModelState.AddModelError("Title", "Title is required");
        SetupUserClaims(1, "Staff");

        // Act
        var result = await _controller.CreateArticle(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        _mockArticleService.Verify(s => s.CreateArticleAsync(It.IsAny<ArticleCreateDTO>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateArticle_WithNullRequest_ShouldReturn400BadRequest()
    {
        // Arrange
        ArticleCreateDTO? request = null;
        SetupUserClaims(1, "Staff");

        // Act
        var result = await _controller.CreateArticle(request!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateArticle_WithEmptyTitle_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = CreateValidArticleCreateDTO();
        request.Title = string.Empty;
        SetupUserClaims(1, "Staff");
        _controller.ModelState.AddModelError("Title", "Title is required");

        // Act
        var result = await _controller.CreateArticle(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateArticle_WithInvalidCategoryId_ShouldReturn404NotFound()
    {
        // Arrange
        var userId = 1;
        var request = CreateValidArticleCreateDTO();
        request.CategoryId = -1;
        SetupUserClaims(userId, "Staff");
        _mockArticleService.Setup(s => s.CreateArticleAsync(It.IsAny<ArticleCreateDTO>(), userId))
            .ThrowsAsync(new KeyNotFoundException("Category not found."));

        // Act
        var result = await _controller.CreateArticle(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Contains("Category not found", errorResponse.Error);
    }

    [Fact]
    public async Task CreateArticle_WithZeroCategoryId_ShouldReturn404NotFound()
    {
        // Arrange
        var userId = 1;
        var request = CreateValidArticleCreateDTO();
        request.CategoryId = 0;
        SetupUserClaims(userId, "Staff");
        _mockArticleService.Setup(s => s.CreateArticleAsync(It.IsAny<ArticleCreateDTO>(), userId))
            .ThrowsAsync(new KeyNotFoundException("Category not found."));

        // Act
        var result = await _controller.CreateArticle(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task CreateArticle_WithMaxIntCategoryId_ShouldHandleGracefully()
    {
        // Arrange
        var userId = 1;
        var request = CreateValidArticleCreateDTO();
        request.CategoryId = int.MaxValue;
        SetupUserClaims(userId, "Staff");
        _mockArticleService.Setup(s => s.CreateArticleAsync(It.IsAny<ArticleCreateDTO>(), userId))
            .ThrowsAsync(new KeyNotFoundException("Category not found."));

        // Act
        var result = await _controller.CreateArticle(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task CreateArticle_WithNullUserIdClaim_ShouldReturn401Unauthorized()
    {
        // Arrange
        var request = CreateValidArticleCreateDTO();
        SetupNullUserClaims();

        // Act
        var result = await _controller.CreateArticle(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
        var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
        Assert.Contains("Invalid token", errorResponse.Error);
    }

    [Fact]
    public async Task CreateArticle_WithInvalidUserIdClaim_ShouldReturn401Unauthorized()
    {
        // Arrange
        var request = CreateValidArticleCreateDTO();
        SetupInvalidUserClaims();

        // Act
        var result = await _controller.CreateArticle(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task CreateArticle_WhenServiceThrowsException_ShouldReturn500InternalServerError()
    {
        // Arrange
        var userId = 1;
        var request = CreateValidArticleCreateDTO();
        SetupUserClaims(userId, "Staff");
        _mockArticleService.Setup(s => s.CreateArticleAsync(It.IsAny<ArticleCreateDTO>(), userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateArticle(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
        Assert.Contains("internal server error", errorResponse.Error);
    }

    #endregion

    #region GetArticleById Tests

    [Fact]
    public async Task GetArticleById_WithValidId_ShouldReturn200OK()
    {
        // Arrange
        var articleId = 1;
        var expectedResponse = CreateArticleResponseDTO(articleId, 1);
        _mockArticleService.Setup(s => s.GetArticleByIdAsync(articleId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetArticleById(articleId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsType<ArticleResponseDTO>(okResult.Value);
        Assert.Equal(articleId, response.ArticleId);
        _mockArticleService.Verify(s => s.GetArticleByIdAsync(articleId), Times.Once);
    }

    [Fact]
    public async Task GetArticleById_WithNonExistentId_ShouldReturn404NotFound()
    {
        // Arrange
        var articleId = 999;
        _mockArticleService.Setup(s => s.GetArticleByIdAsync(articleId))
            .ReturnsAsync((ArticleResponseDTO?)null);

        // Act
        var result = await _controller.GetArticleById(articleId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetArticleById_WithZeroId_ShouldReturn404NotFound()
    {
        // Arrange
        var articleId = 0;
        _mockArticleService.Setup(s => s.GetArticleByIdAsync(articleId))
            .ReturnsAsync((ArticleResponseDTO?)null);

        // Act
        var result = await _controller.GetArticleById(articleId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetArticleById_WithNegativeId_ShouldReturn404NotFound()
    {
        // Arrange
        var articleId = -1;
        _mockArticleService.Setup(s => s.GetArticleByIdAsync(articleId))
            .ReturnsAsync((ArticleResponseDTO?)null);

        // Act
        var result = await _controller.GetArticleById(articleId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetArticleById_WithMaxIntId_ShouldHandleGracefully()
    {
        // Arrange
        var articleId = int.MaxValue;
        _mockArticleService.Setup(s => s.GetArticleByIdAsync(articleId))
            .ReturnsAsync((ArticleResponseDTO?)null);

        // Act
        var result = await _controller.GetArticleById(articleId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    #endregion

    #region GetPublicArticles Tests

    [Fact]
    public async Task GetPublicArticles_WithValidRequest_ShouldReturn200OK()
    {
        // Arrange
        var expectedArticles = new List<ArticleResponseDTO>
        {
            CreateArticleResponseDTO(1, 1),
            CreateArticleResponseDTO(2, 1)
        };
        var expectedResult = new PagedResponse<ArticleResponseDTO>
        {
            Items = expectedArticles,
            Total = 2,
            Page = 1,
            PageSize = 1000
        };
        _mockArticleService.Setup(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
            q.IsPublished == true && q.Status == "Published")))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPublicArticles();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var articles = Assert.IsType<List<ArticleResponseDTO>>(okResult.Value);
        Assert.Equal(2, articles.Count);
        _mockArticleService.Verify(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
            q.IsPublished == true && q.Status == "Published")), Times.Once);
    }

    [Fact]
    public async Task GetPublicArticles_WhenServiceThrowsException_ShouldReturn500InternalServerError()
    {
        // Arrange
        _mockArticleService.Setup(s => s.QueryAsync(It.IsAny<ArticleQueryParams>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetPublicArticles();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
        Assert.Contains("internal server error", errorResponse.Error);
    }

    #endregion

    #region GetMyArticleById Tests

    [Fact]
    public async Task GetMyArticleById_WithValidIdAndOwnArticle_ShouldReturn200OK()
    {
        // Arrange
        var userId = 1;
        var articleId = 1;
        var staffUser = CreateUser(userId, 3);
        var article = CreateArticle(articleId, userId);
        var category = CreateCategory(1);
        var author = CreateUser(userId, 3);

        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
        _mockCategoryRepository.Setup(r => r.FindByIdAsync(It.IsAny<int>())).ReturnsAsync(category);
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(author);

        // Act
        var result = await _controller.GetMyArticleById(articleId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsType<ArticleResponseDTO>(okResult.Value);
        Assert.Equal(articleId, response.ArticleId);
    }

    [Fact]
    public async Task GetMyArticleById_WithNonExistentId_ShouldReturn404NotFound()
    {
        // Arrange
        var userId = 1;
        var articleId = 999;
        var staffUser = CreateUser(userId, 3);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync((Article?)null);

        // Act
        var result = await _controller.GetMyArticleById(articleId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetMyArticleById_WithOtherUserArticle_ShouldReturn403Forbid()
    {
        // Arrange
        var userId = 1;
        var otherUserId = 2;
        var articleId = 1;
        var staffUser = CreateUser(userId, 3);
        var article = CreateArticle(articleId, otherUserId);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);

        // Act
        var result = await _controller.GetMyArticleById(articleId);

        // Assert
        var forbidResult = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetMyArticleById_WithNullUserIdClaim_ShouldReturn401Unauthorized()
    {
        // Arrange
        var articleId = 1;
        SetupNullUserClaims();

        // Act
        var result = await _controller.GetMyArticleById(articleId);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task GetMyArticleById_WithZeroId_ShouldReturn404NotFound()
    {
        // Arrange
        var userId = 1;
        var articleId = 0;
        var staffUser = CreateUser(userId, 3);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync((Article?)null);

        // Act
        var result = await _controller.GetMyArticleById(articleId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetMyArticleById_WhenServiceThrowsException_ShouldReturn500InternalServerError()
    {
        // Arrange
        var userId = 1;
        var articleId = 1;
        var staffUser = CreateUser(userId, 3);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetMyArticleById(articleId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    #endregion

    #region GetCategories Tests

    [Fact]
    public async Task GetCategories_WithValidRequest_ShouldReturn200OK()
    {
        // Arrange
        var categories = new List<ArticleCategory>
        {
            CreateCategory(1),
            CreateCategory(2)
        };
        _mockCategoryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

        // Act
        var result = await _controller.GetCategories();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsType<List<object>>(okResult.Value);
        Assert.Equal(2, response.Count);
        _mockCategoryRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCategories_WithEmptyList_ShouldReturn200OK()
    {
        // Arrange
        var categories = new List<ArticleCategory>();
        _mockCategoryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

        // Act
        var result = await _controller.GetCategories();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsType<List<object>>(okResult.Value);
        Assert.Empty(response);
    }

    #endregion

    #region GetAllArticles Tests

    [Fact]
    public async Task GetAllArticles_AsAnonymous_ShouldReturn200OKWithPublishedOnly()
    {
        // Arrange
        var expectedArticles = new List<ArticleResponseDTO>
        {
            CreateArticleResponseDTO(1, 1)
        };
        var expectedResult = new PagedResponse<ArticleResponseDTO>
        {
            Items = expectedArticles,
            Total = 1,
            Page = 1,
            PageSize = 1000
        };
        SetupNullUserClaims();
        _mockArticleService.Setup(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
            q.IsPublished == true && q.Status == "Published")))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAllArticles();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var articles = Assert.IsType<List<ArticleResponseDTO>>(okResult.Value);
        Assert.Single(articles);
    }

    [Fact]
    public async Task GetAllArticles_AsStaff_ShouldReturn200OKWithOwnArticles()
    {
        // Arrange
        var userId = 1;
        var staffUser = CreateUser(userId, 3);
        var expectedArticles = new List<ArticleResponseDTO>
        {
            CreateArticleResponseDTO(1, userId)
        };
        var expectedResult = new PagedResponse<ArticleResponseDTO>
        {
            Items = expectedArticles,
            Total = 1,
            Page = 1,
            PageSize = 1000
        };
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleService.Setup(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
            q.CreatedBy == userId)))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAllArticles();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var articles = Assert.IsType<List<ArticleResponseDTO>>(okResult.Value);
        Assert.Single(articles);
    }

    [Fact]
    public async Task GetAllArticles_AsManager_ShouldReturn200OKWithAllArticles()
    {
        // Arrange
        var userId = 2;
        var managerUser = CreateUser(userId, 2);
        var expectedArticles = new List<ArticleResponseDTO>
        {
            CreateArticleResponseDTO(1, 1),
            CreateArticleResponseDTO(2, 2)
        };
        SetupUserClaims(userId, "Manager");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(managerUser);
        _mockArticleService.Setup(s => s.GetAllArticlesAsync())
            .ReturnsAsync(expectedArticles);

        // Act
        var result = await _controller.GetAllArticles();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var articles = Assert.IsType<List<ArticleResponseDTO>>(okResult.Value);
        Assert.Equal(2, articles.Count);
    }

    [Fact]
    public async Task GetAllArticles_WhenServiceThrowsException_ShouldReturn500InternalServerError()
    {
        // Arrange
        SetupNullUserClaims();
        _mockArticleService.Setup(s => s.QueryAsync(It.IsAny<ArticleQueryParams>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAllArticles();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task Query_AsAnonymous_ShouldReturn200OKWithPublishedOnly()
    {
        // Arrange
        var query = new ArticleQueryParams { Page = 1, PageSize = 10 };
        var expectedResult = new PagedResponse<ArticleResponseDTO>
        {
            Items = new List<ArticleResponseDTO> { CreateArticleResponseDTO(1, 1) },
            Total = 1,
            Page = 1,
            PageSize = 10
        };
        SetupNullUserClaims();
        _mockArticleService.Setup(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
            q.IsPublished == true && q.Status == "Published")))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Query(query);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.True(query.IsPublished == true);
        Assert.Equal("Published", query.Status);
    }

    [Fact]
    public async Task Query_AsStaff_ShouldFilterByCreatedBy()
    {
        // Arrange
        var userId = 1;
        var staffUser = CreateUser(userId, 3);
        var query = new ArticleQueryParams { Page = 1, PageSize = 10 };
        var expectedResult = new PagedResponse<ArticleResponseDTO>
        {
            Items = new List<ArticleResponseDTO> { CreateArticleResponseDTO(1, userId) },
            Total = 1,
            Page = 1,
            PageSize = 10
        };
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleService.Setup(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
            q.CreatedBy == userId)))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Query(query);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Equal(userId, query.CreatedBy);
    }

    [Fact]
    public async Task Query_WithZeroPage_ShouldHandleGracefully()
    {
        // Arrange
        var query = new ArticleQueryParams { Page = 0, PageSize = 10 };
        var expectedResult = new PagedResponse<ArticleResponseDTO>
        {
            Items = new List<ArticleResponseDTO>(),
            Total = 0,
            Page = 0,
            PageSize = 10
        };
        SetupNullUserClaims();
        _mockArticleService.Setup(s => s.QueryAsync(It.IsAny<ArticleQueryParams>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Query(query);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task Query_WithNegativePageSize_ShouldHandleGracefully()
    {
        // Arrange
        var query = new ArticleQueryParams { Page = 1, PageSize = -1 };
        var expectedResult = new PagedResponse<ArticleResponseDTO>
        {
            Items = new List<ArticleResponseDTO>(),
            Total = 0,
            Page = 1,
            PageSize = -1
        };
        SetupNullUserClaims();
        _mockArticleService.Setup(s => s.QueryAsync(It.IsAny<ArticleQueryParams>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Query(query);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task Query_WithEmptySearchString_ShouldReturn200OK()
    {
        // Arrange
        var query = new ArticleQueryParams { Page = 1, PageSize = 10, Search = string.Empty };
        var expectedResult = new PagedResponse<ArticleResponseDTO>
        {
            Items = new List<ArticleResponseDTO>(),
            Total = 0,
            Page = 1,
            PageSize = 10
        };
        SetupNullUserClaims();
        _mockArticleService.Setup(s => s.QueryAsync(It.IsAny<ArticleQueryParams>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Query(query);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task Query_WithNullQuery_ShouldHandleGracefully()
    {
        // Arrange
        ArticleQueryParams? query = null;
        var expectedResult = new PagedResponse<ArticleResponseDTO>
        {
            Items = new List<ArticleResponseDTO>(),
            Total = 0,
            Page = 1,
            PageSize = 10
        };
        SetupNullUserClaims();
        _mockArticleService.Setup(s => s.QueryAsync(It.IsAny<ArticleQueryParams>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Query(query!);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task Query_WhenServiceThrowsException_ShouldReturn500InternalServerError()
    {
        // Arrange
        var query = new ArticleQueryParams { Page = 1, PageSize = 10 };
        SetupNullUserClaims();
        _mockArticleService.Setup(s => s.QueryAsync(It.IsAny<ArticleQueryParams>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Query(query);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidRequest_ShouldReturn200OK()
    {
        // Arrange
        var userId = 1;
        var articleId = 1;
        var request = CreateValidArticleUpdateDTO();
        var staffUser = CreateUser(userId, 3);
        var article = CreateArticle(articleId, userId);
        var expectedResponse = CreateArticleResponseDTO(articleId, userId);

        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
        _mockArticleService.Setup(s => s.UpdateArticleAsync(articleId, request, userId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Update(articleId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsType<ArticleResponseDTO>(okResult.Value);
        Assert.Equal(articleId, response.ArticleId);
        _mockArticleService.Verify(s => s.UpdateArticleAsync(articleId, request, userId), Times.Once);
    }

    [Fact]
    public async Task Update_WithNonExistentId_ShouldReturn404NotFound()
    {
        // Arrange
        var userId = 1;
        var articleId = 999;
        var request = CreateValidArticleUpdateDTO();
        var staffUser = CreateUser(userId, 3);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync((Article?)null);

        // Act
        var result = await _controller.Update(articleId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task Update_WithOtherUserArticle_ShouldReturn403Forbid()
    {
        // Arrange
        var userId = 1;
        var otherUserId = 2;
        var articleId = 1;
        var request = CreateValidArticleUpdateDTO();
        var staffUser = CreateUser(userId, 3);
        var article = CreateArticle(articleId, otherUserId);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);

        // Act
        var result = await _controller.Update(articleId, request);

        // Assert
        var forbidResult = Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Update_WithNullRequest_ShouldHandleGracefully()
    {
        // Arrange
        var userId = 1;
        var articleId = 1;
        ArticleUpdateDTO? request = null;
        var staffUser = CreateUser(userId, 3);
        var article = CreateArticle(articleId, userId);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
        _mockArticleService.Setup(s => s.UpdateArticleAsync(articleId, It.IsAny<ArticleUpdateDTO>(), userId))
            .ReturnsAsync((ArticleResponseDTO?)null);

        // Act
        var result = await _controller.Update(articleId, request!);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task Update_WithEmptyTitle_ShouldReturn200OK()
    {
        // Arrange
        var userId = 1;
        var articleId = 1;
        var request = CreateValidArticleUpdateDTO();
        request.Title = string.Empty;
        var staffUser = CreateUser(userId, 3);
        var article = CreateArticle(articleId, userId);
        var expectedResponse = CreateArticleResponseDTO(articleId, userId);
        expectedResponse.Title = string.Empty;

        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
        _mockArticleService.Setup(s => s.UpdateArticleAsync(articleId, request, userId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Update(articleId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task Update_WithZeroId_ShouldReturn404NotFound()
    {
        // Arrange
        var userId = 1;
        var articleId = 0;
        var request = CreateValidArticleUpdateDTO();
        var staffUser = CreateUser(userId, 3);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync((Article?)null);

        // Act
        var result = await _controller.Update(articleId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task Update_WithNullUserIdClaim_ShouldReturn401Unauthorized()
    {
        // Arrange
        var articleId = 1;
        var request = CreateValidArticleUpdateDTO();
        SetupNullUserClaims();

        // Act
        var result = await _controller.Update(articleId, request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    #endregion

    #region RequestApproval Tests

    [Fact]
    public async Task RequestApproval_WithValidRequest_ShouldReturn204NoContent()
    {
        // Arrange
        var userId = 1;
        var articleId = 1;
        var staffUser = CreateUser(userId, 3);
        var article = CreateArticle(articleId, userId);
        article.Status = "Draft";
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
        _mockArticleService.Setup(s => s.RequestApprovalAsync(articleId, userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.RequestApproval(articleId);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
        _mockArticleService.Verify(s => s.RequestApprovalAsync(articleId, userId), Times.Once);
    }

    [Fact]
    public async Task RequestApproval_WithNonExistentId_ShouldReturn404NotFound()
    {
        // Arrange
        var userId = 1;
        var articleId = 999;
        var staffUser = CreateUser(userId, 3);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync((Article?)null);

        // Act
        var result = await _controller.RequestApproval(articleId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task RequestApproval_WithOtherUserArticle_ShouldReturn403Forbid()
    {
        // Arrange
        var userId = 1;
        var otherUserId = 2;
        var articleId = 1;
        var staffUser = CreateUser(userId, 3);
        var article = CreateArticle(articleId, otherUserId);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);

        // Act
        var result = await _controller.RequestApproval(articleId);

        // Assert
        var forbidResult = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task RequestApproval_WhenServiceReturnsFalse_ShouldReturn404NotFound()
    {
        // Arrange
        var userId = 1;
        var articleId = 1;
        var staffUser = CreateUser(userId, 3);
        var article = CreateArticle(articleId, userId);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
        _mockArticleService.Setup(s => s.RequestApprovalAsync(articleId, userId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.RequestApproval(articleId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task RequestApproval_WithZeroId_ShouldReturn404NotFound()
    {
        // Arrange
        var userId = 1;
        var articleId = 0;
        var staffUser = CreateUser(userId, 3);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync((Article?)null);

        // Act
        var result = await _controller.RequestApproval(articleId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    

    #endregion

    #region ReviewArticle Tests

    [Fact]
    public async Task ReviewArticle_WithValidRequest_ShouldReturn204NoContent()
    {
        // Arrange
        var userId = 2;
        var articleId = 1;
        var request = new ArticleReviewRequest { IsApproved = true, Comment = "Approved" };
        SetupUserClaims(userId, "Manager");
        _mockArticleService.Setup(s => s.ReviewArticleAsync(articleId, request, userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ReviewArticle(articleId, request);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
        _mockArticleService.Verify(s => s.ReviewArticleAsync(articleId, request, userId), Times.Once);
    }

    [Fact]
    public async Task ReviewArticle_WhenServiceReturnsFalse_ShouldReturn404NotFound()
    {
        // Arrange
        var userId = 2;
        var articleId = 1;
        var request = new ArticleReviewRequest { IsApproved = true, Comment = "Approved" };
        SetupUserClaims(userId, "Manager");
        _mockArticleService.Setup(s => s.ReviewArticleAsync(articleId, request, userId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ReviewArticle(articleId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task ReviewArticle_WithNullRequest_ShouldHandleGracefully()
    {
        // Arrange
        var userId = 2;
        var articleId = 1;
        ArticleReviewRequest? request = null;
        SetupUserClaims(userId, "Manager");
        _mockArticleService.Setup(s => s.ReviewArticleAsync(articleId, It.IsAny<ArticleReviewRequest>(), userId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ReviewArticle(articleId, request!);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task ReviewArticle_WithEmptyComment_ShouldReturn204NoContent()
    {
        // Arrange
        var userId = 2;
        var articleId = 1;
        var request = new ArticleReviewRequest { IsApproved = true, Comment = string.Empty };
        SetupUserClaims(userId, "Manager");
        _mockArticleService.Setup(s => s.ReviewArticleAsync(articleId, request, userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ReviewArticle(articleId, request);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
    }

    [Fact]
    public async Task ReviewArticle_WithZeroId_ShouldReturn404NotFound()
    {
        // Arrange
        var userId = 2;
        var articleId = 0;
        var request = new ArticleReviewRequest { IsApproved = true, Comment = "Approved" };
        SetupUserClaims(userId, "Manager");
        _mockArticleService.Setup(s => s.ReviewArticleAsync(articleId, request, userId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ReviewArticle(articleId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task ReviewArticle_WithNullUserIdClaim_ShouldReturn401Unauthorized()
    {
        // Arrange
        var articleId = 1;
        var request = new ArticleReviewRequest { IsApproved = true, Comment = "Approved" };
        SetupNullUserClaims();

        // Act
        var result = await _controller.ReviewArticle(articleId, request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    #endregion

    #region DeleteArticle Tests

    [Fact]
    public async Task DeleteArticle_WithValidRequest_ShouldReturn204NoContent()
    {
        // Arrange
        var userId = 1;
        var articleId = 1;
        var staffUser = CreateUser(userId, 3);
        var article = CreateArticle(articleId, userId);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
        _mockArticleService.Setup(s => s.DeleteArticleAsync(articleId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteArticle(articleId);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
        _mockArticleService.Verify(s => s.DeleteArticleAsync(articleId), Times.Once);
    }

    [Fact]
    public async Task DeleteArticle_WithNonExistentId_ShouldReturn404NotFound()
    {
        // Arrange
        var userId = 1;
        var articleId = 999;
        var staffUser = CreateUser(userId, 3);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync((Article?)null);

        // Act
        var result = await _controller.DeleteArticle(articleId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task DeleteArticle_WithOtherUserArticle_ShouldReturn403Forbid()
    {
        // Arrange
        var userId = 1;
        var otherUserId = 2;
        var articleId = 1;
        var staffUser = CreateUser(userId, 3);
        var article = CreateArticle(articleId, otherUserId);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);

        // Act
        var result = await _controller.DeleteArticle(articleId);

        // Assert
        var forbidResult = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteArticle_WhenServiceReturnsFalse_ShouldReturn404NotFound()
    {
        // Arrange
        var userId = 1;
        var articleId = 1;
        var staffUser = CreateUser(userId, 3);
        var article = CreateArticle(articleId, userId);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
        _mockArticleService.Setup(s => s.DeleteArticleAsync(articleId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteArticle(articleId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task DeleteArticle_WithZeroId_ShouldReturn404NotFound()
    {
        // Arrange
        var userId = 1;
        var articleId = 0;
        var staffUser = CreateUser(userId, 3);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync((Article?)null);

        // Act
        var result = await _controller.DeleteArticle(articleId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task DeleteArticle_WithNullUserIdClaim_ShouldReturn401Unauthorized()
    {
        // Arrange
        var articleId = 1;
        SetupNullUserClaims();

        // Act
        var result = await _controller.DeleteArticle(articleId);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task DeleteArticle_WhenServiceThrowsException_ShouldReturn500InternalServerError()
    {
        // Arrange
        var userId = 1;
        var articleId = 1;
        var staffUser = CreateUser(userId, 3);
        var article = CreateArticle(articleId, userId);
        SetupUserClaims(userId, "Staff");
        _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
        _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
        _mockArticleService.Setup(s => s.DeleteArticleAsync(articleId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteArticle(articleId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    #endregion

    #region Helper Methods

    private void SetupUserClaims(int userId, string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private void SetupNullUserClaims()
    {
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private void SetupInvalidUserClaims()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "invalid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private ArticleCreateDTO CreateValidArticleCreateDTO()
    {
        return new ArticleCreateDTO
        {
            Title = "Test Article",
            Summary = "Test Summary",
            CategoryId = 1,
            PublishNow = false,
            Sections = new List<ArticleSectionCreateDTO>
            {
                new ArticleSectionCreateDTO
                {
                    SectionTitle = "Section 1",
                    SectionContent = "Content 1",
                    OrderIndex = 0
                }
            }
        };
    }

    private ArticleUpdateDTO CreateValidArticleUpdateDTO()
    {
        return new ArticleUpdateDTO
        {
            Title = "Updated Article",
            Summary = "Updated Summary",
            CategoryId = 1,
            Sections = new List<ArticleSectionUpdateDTO>
            {
                new ArticleSectionUpdateDTO
                {
                    SectionTitle = "Updated Section",
                    SectionContent = "Updated Content",
                    OrderIndex = 0
                }
            }
        };
    }

    private ArticleResponseDTO CreateArticleResponseDTO(int articleId, int userId)
    {
        return new ArticleResponseDTO
        {
            ArticleId = articleId,
            Title = "Test Article",
            Summary = "Test Summary",
            IsPublished = false,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow,
            AuthorName = "Test Author",
            CategoryName = "Test Category",
            Sections = new List<ArticleSectionResponseDTO>
            {
                new ArticleSectionResponseDTO
                {
                    SectionId = 1,
                    SectionTitle = "Section 1",
                    SectionContent = "Content 1",
                    OrderIndex = 0
                }
            }
        };
    }

    private User CreateUser(int userId, int roleId)
    {
        return new User
        {
            UserId = userId,
            RoleId = roleId,
            Email = $"user{userId}@test.com",
            FullName = $"User {userId}"
        };
    }

    private Article CreateArticle(int articleId, int createdBy)
    {
        return new Article
        {
            ArticleId = articleId,
            Title = "Test Article",
            Summary = "Test Summary",
            CategoryId = 1,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsPublished = false,
            Status = "Draft",
            ArticleSections = new List<ArticleSection>()
        };
    }

    private ArticleCategory CreateCategory(int categoryId)
    {
        return new ArticleCategory
        {
            CategoryId = categoryId,
            CategoryName = "Test Category",
            Description = "Test Description",
            CreateAt = DateTime.UtcNow
        };
    }

    #endregion
}

