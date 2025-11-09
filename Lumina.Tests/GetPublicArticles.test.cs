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

namespace Lumina.Tests
{
    public class GetPublicArticlesTests
    {
        private readonly Mock<IArticleService> _mockArticleService;
        private readonly Mock<ILogger<ArticlesController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly ArticlesController _controller;

        public GetPublicArticlesTests()
        {
            _mockArticleService = new Mock<IArticleService>();
            _mockLogger = new Mock<ILogger<ArticlesController>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _controller = new ArticlesController(_mockArticleService.Object, _mockLogger.Object, _mockUnitOfWork.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task GetPublicArticles_ShouldReturn200OkWithPublishedArticles()
        {
            // Arrange
            var expectedArticles = new List<ArticleResponseDTO>
            {
                new ArticleResponseDTO
                {
                    ArticleId = 1,
                    Title = "Published Article 1",
                    Summary = "Summary 1",
                    IsPublished = true,
                    Status = "Published"
                },
                new ArticleResponseDTO
                {
                    ArticleId = 2,
                    Title = "Published Article 2",
                    Summary = "Summary 2",
                    IsPublished = true,
                    Status = "Published"
                }
            };

            var queryResult = new PagedResponse<ArticleResponseDTO>
            {
                Items = expectedArticles,
                Total = 2,
                Page = 1,
                PageSize = 1000
            };

            _mockArticleService.Setup(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
                q.Page == 1 &&
                q.PageSize == 1000 &&
                q.IsPublished == true &&
                q.Status == "Published"
            ))).ReturnsAsync(queryResult);

            // Act
            var result = await _controller.GetPublicArticles();

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<ArticleResponseDTO>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var articles = Assert.IsType<List<ArticleResponseDTO>>(okResult.Value);
            Assert.Equal(2, articles.Count);
            _mockArticleService.Verify(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
                q.Page == 1 &&
                q.PageSize == 1000 &&
                q.IsPublished == true &&
                q.Status == "Published"
            )), Times.Once);
        }

        [Fact]
        public async Task GetPublicArticles_WithNoPublishedArticles_ShouldReturn200OkWithEmptyList()
        {
            // Arrange
            var queryResult = new PagedResponse<ArticleResponseDTO>
            {
                Items = new List<ArticleResponseDTO>(),
                Total = 0,
                Page = 1,
                PageSize = 1000
            };

            _mockArticleService.Setup(s => s.QueryAsync(It.IsAny<ArticleQueryParams>())).ReturnsAsync(queryResult);

            // Act
            var result = await _controller.GetPublicArticles();

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<ArticleResponseDTO>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var articles = Assert.IsType<List<ArticleResponseDTO>>(okResult.Value);
            Assert.Empty(articles);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task GetPublicArticles_WhenServiceThrowsException_ShouldReturn500InternalServerError()
        {
            // Arrange
            _mockArticleService.Setup(s => s.QueryAsync(It.IsAny<ArticleQueryParams>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetPublicArticles();

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<ArticleResponseDTO>>>(result);
            var statusCodeResult = Assert.IsType<ObjectResult>(actionResult.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("An internal server error occurred. Please try again later.", errorResponse.Error);
        }

        #endregion
    }
}

