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
    public class ReviewArticleTests
    {
        private readonly Mock<IArticleService> _mockArticleService;
        private readonly Mock<ILogger<ArticlesController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly ArticlesController _controller;

        public ReviewArticleTests()
        {
            _mockArticleService = new Mock<IArticleService>();
            _mockLogger = new Mock<ILogger<ArticlesController>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _controller = new ArticlesController(_mockArticleService.Object, _mockLogger.Object, _mockUnitOfWork.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task ReviewArticle_WithApproval_ShouldReturn204NoContent()
        {
            // Arrange
            var userId = 1;
            var articleId = 1;
            var request = new ArticleReviewRequest
            {
                IsApproved = true,
                Comment = "Good article"
            };

            SetupUserClaims(userId);
            _mockArticleService.Setup(s => s.ReviewArticleAsync(articleId, request, userId)).ReturnsAsync(true);

            // Act
            var result = await _controller.ReviewArticle(articleId, request);

            // Assert
            var noContentResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
            _mockArticleService.Verify(s => s.ReviewArticleAsync(articleId, request, userId), Times.Once);
        }

        [Fact]
        public async Task ReviewArticle_WithRejection_ShouldReturn204NoContent()
        {
            // Arrange
            var userId = 1;
            var articleId = 1;
            var request = new ArticleReviewRequest
            {
                IsApproved = false,
                Comment = "Needs improvement"
            };

            SetupUserClaims(userId);
            _mockArticleService.Setup(s => s.ReviewArticleAsync(articleId, request, userId)).ReturnsAsync(true);

            // Act
            var result = await _controller.ReviewArticle(articleId, request);

            // Assert
            var noContentResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
            _mockArticleService.Verify(s => s.ReviewArticleAsync(articleId, request, userId), Times.Once);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task ReviewArticle_WithNullUserIdClaim_ShouldReturn401Unauthorized()
        {
            // Arrange
            var articleId = 1;
            var request = new ArticleReviewRequest
            {
                IsApproved = true,
                Comment = "Good article"
            };
            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.ReviewArticle(articleId, request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid token.", errorResponse.Error);
        }

        #endregion

        #region Not Found Tests

        [Fact]
        public async Task ReviewArticle_WhenServiceReturnsFalse_ShouldReturn404NotFound()
        {
            // Arrange
            var userId = 1;
            var articleId = 999;
            var request = new ArticleReviewRequest
            {
                IsApproved = true,
                Comment = "Good article"
            };

            SetupUserClaims(userId);
            _mockArticleService.Setup(s => s.ReviewArticleAsync(articleId, request, userId)).ReturnsAsync(false);

            // Act
            var result = await _controller.ReviewArticle(articleId, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Equal($"Article with ID {articleId} not found or is not pending review.", errorResponse.Error);
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

