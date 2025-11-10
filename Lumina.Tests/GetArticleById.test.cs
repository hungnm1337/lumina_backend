using DataLayer.DTOs;
using DataLayer.DTOs.Article;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Article;

namespace Lumina.Tests
{
    public class GetArticleByIdTests
    {
        private readonly Mock<IArticleService> _mockArticleService;
        private readonly Mock<ILogger<ArticlesController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly ArticlesController _controller;

        public GetArticleByIdTests()
        {
            _mockArticleService = new Mock<IArticleService>();
            _mockLogger = new Mock<ILogger<ArticlesController>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _controller = new ArticlesController(_mockArticleService.Object, _mockLogger.Object, _mockUnitOfWork.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task GetArticleById_WithValidId_ShouldReturn200Ok()
        {
            // Arrange
            var articleId = 1;
            var expectedResponse = new ArticleResponseDTO
            {
                ArticleId = 1,
                Title = "Test Article",
                Summary = "This is a test article",
                IsPublished = true,
                Status = "Published"
            };

            _mockArticleService.Setup(s => s.GetArticleByIdAsync(articleId)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetArticleById(articleId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.Equal(expectedResponse, okResult.Value);
            _mockArticleService.Verify(s => s.GetArticleByIdAsync(articleId), Times.Once);
        }

        #endregion

        #region Not Found Tests

        [Fact]
        public async Task GetArticleById_WithNonExistentId_ShouldReturn404NotFound()
        {
            // Arrange
            var articleId = 999;

            _mockArticleService.Setup(s => s.GetArticleByIdAsync(articleId)).ReturnsAsync((ArticleResponseDTO?)null);

            // Act
            var result = await _controller.GetArticleById(articleId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            _mockArticleService.Verify(s => s.GetArticleByIdAsync(articleId), Times.Once);
        }

        #endregion
    }
}

