using DataLayer.Models;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Article;
using System;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class DeleteArticleServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly ArticleService _service;

        public DeleteArticleServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockArticleRepository = new Mock<IArticleRepository>();

            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task DeleteArticleAsync_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var articleId = 1;
            var article = new Article
            {
                ArticleId = articleId,
                Title = "Test Article",
                Summary = "Test Summary"
            };

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockArticleRepository.Setup(r => r.DeleteAsync(articleId)).ReturnsAsync(true);

            // Act
            var result = await _service.DeleteArticleAsync(articleId);

            // Assert
            Assert.True(result);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
            _mockArticleRepository.Verify(r => r.DeleteAsync(articleId), Times.Once);
        }

        [Fact]
        public async Task DeleteArticleAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var articleId = 999;

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync((Article?)null);

            // Act
            var result = await _service.DeleteArticleAsync(articleId);

            // Assert
            Assert.False(result);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
            _mockArticleRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteArticleAsync_WhenDeleteThrowsException_ShouldReturnFalse()
        {
            // Arrange
            var articleId = 1;
            var article = new Article
            {
                ArticleId = articleId,
                Title = "Test Article",
                Summary = "Test Summary"
            };

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockArticleRepository.Setup(r => r.DeleteAsync(articleId)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.DeleteArticleAsync(articleId);

            // Assert
            Assert.False(result);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
            _mockArticleRepository.Verify(r => r.DeleteAsync(articleId), Times.Once);
        }
    }
}

