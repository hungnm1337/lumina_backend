using DataLayer.DTOs;
using DataLayer.DTOs.Article;
using DataLayer.DTOs.Auth;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Article;
using System.Security.Claims;

namespace Lumina.Tests
{
    public class CreateArticleTests
    {
        private readonly Mock<IArticleService> _mockArticleService;
        private readonly Mock<ILogger<ArticlesController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly ArticlesController _controller;

        public CreateArticleTests()
        {
            _mockArticleService = new Mock<IArticleService>();
            _mockLogger = new Mock<ILogger<ArticlesController>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _controller = new ArticlesController(_mockArticleService.Object, _mockLogger.Object, _mockUnitOfWork.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task CreateArticle_WithValidInput_ShouldReturn201Created()
        {
            // Arrange
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "This is a test article",
                CategoryId = 1,
                PublishNow = false,
                Sections = new List<ArticleSectionCreateDTO>
                {
                    new ArticleSectionCreateDTO
                    {
                        SectionTitle = "Introduction",
                        SectionContent = "This is the introduction",
                        OrderIndex = 1
                    }
                }
            };
            var userId = 1;
            var expectedResponse = new ArticleResponseDTO
            {
                ArticleId = 1,
                Title = "Test Article",
                Summary = "This is a test article",
                IsPublished = false,
                Status = "Draft"
            };

            SetupUserClaims(userId);
            _mockArticleService.Setup(s => s.CreateArticleAsync(request, userId)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateArticle(request);

            // Assert
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(StatusCodes.Status201Created, createdAtResult.StatusCode);
            Assert.Equal(nameof(ArticlesController.GetArticleById), createdAtResult.ActionName);
            Assert.Equal(expectedResponse, createdAtResult.Value);
            _mockArticleService.Verify(s => s.CreateArticleAsync(request, userId), Times.Once);
        }

        [Fact]
        public async Task CreateArticle_WithPublishNow_ShouldReturn201Created()
        {
            // Arrange
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "This is a test article",
                CategoryId = 1,
                PublishNow = true,
                Sections = new List<ArticleSectionCreateDTO>()
            };
            var userId = 1;
            var expectedResponse = new ArticleResponseDTO
            {
                ArticleId = 1,
                Title = "Test Article",
                Summary = "This is a test article",
                IsPublished = true,
                Status = "Published"
            };

            SetupUserClaims(userId);
            _mockArticleService.Setup(s => s.CreateArticleAsync(request, userId)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateArticle(request);

            // Assert
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(StatusCodes.Status201Created, createdAtResult.StatusCode);
            _mockArticleService.Verify(s => s.CreateArticleAsync(request, userId), Times.Once);
        }

        #endregion

        #region ModelState Validation Tests

        [Fact]
        public async Task CreateArticle_WithInvalidModelState_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new ArticleCreateDTO
            {
                Title = "",
                Summary = "",
                CategoryId = 0
            };
            _controller.ModelState.AddModelError("Title", "Title is required");

            // Act
            var result = await _controller.CreateArticle(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            _mockArticleService.Verify(s => s.CreateArticleAsync(It.IsAny<ArticleCreateDTO>(), It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task CreateArticle_WithNullUserIdClaim_ShouldReturn401Unauthorized()
        {
            // Arrange
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "This is a test article",
                CategoryId = 1
            };
            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.CreateArticle(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid token - User ID could not be determined.", errorResponse.Error);
        }

        [Fact]
        public async Task CreateArticle_WithInvalidUserIdClaim_ShouldReturn401Unauthorized()
        {
            // Arrange
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "This is a test article",
                CategoryId = 1
            };
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "invalid-number")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.CreateArticle(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid token - User ID could not be determined.", errorResponse.Error);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task CreateArticle_WhenCategoryNotFound_ShouldReturn404NotFound()
        {
            // Arrange
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "This is a test article",
                CategoryId = 999
            };
            var userId = 1;

            SetupUserClaims(userId);
            _mockArticleService.Setup(s => s.CreateArticleAsync(request, userId))
                .ThrowsAsync(new KeyNotFoundException("Category not found."));

            // Act
            var result = await _controller.CreateArticle(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Equal("Category not found.", errorResponse.Error);
        }

        [Fact]
        public async Task CreateArticle_WhenServiceThrowsException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "This is a test article",
                CategoryId = 1
            };
            var userId = 1;

            SetupUserClaims(userId);
            _mockArticleService.Setup(s => s.CreateArticleAsync(request, userId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateArticle(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("An internal server error occurred. Please try again later.", errorResponse.Error);
        }

        #endregion

        #region Helper Methods

        private void SetupUserClaims(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        #endregion
    }
}

