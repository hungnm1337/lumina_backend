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
    public class RequestApprovalArticleServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly ArticleService _service;

        public RequestApprovalArticleServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockArticleRepository = new Mock<IArticleRepository>();

            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task RequestApprovalAsync_WithDraftStatus_ShouldReturnTrue()
        {
            // Arrange
            var articleId = 1;
            var staffUserId = 1;
            var article = new Article
            {
                ArticleId = articleId,
                Title = "Draft Article",
                Summary = "Summary",
                Status = "Draft",
                IsPublished = false,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow
            };

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockArticleRepository.Setup(r => r.UpdateAsync(It.IsAny<Article>())).ReturnsAsync(article);

            // Act
            var result = await _service.RequestApprovalAsync(articleId, staffUserId);

            // Assert
            Assert.True(result);
            Assert.Equal("Pending", article.Status);
            Assert.False(article.IsPublished);
            Assert.Equal(staffUserId, article.UpdatedBy);
            Assert.NotNull(article.UpdatedAt);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
            _mockArticleRepository.Verify(r => r.UpdateAsync(article), Times.Once);
        }

        [Fact]
        public async Task RequestApprovalAsync_WithRejectedStatus_ShouldReturnTrue()
        {
            // Arrange
            var articleId = 1;
            var staffUserId = 1;
            var article = new Article
            {
                ArticleId = articleId,
                Title = "Rejected Article",
                Summary = "Summary",
                Status = "Rejected",
                IsPublished = false,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow
            };

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockArticleRepository.Setup(r => r.UpdateAsync(It.IsAny<Article>())).ReturnsAsync(article);

            // Act
            var result = await _service.RequestApprovalAsync(articleId, staffUserId);

            // Assert
            Assert.True(result);
            Assert.Equal("Pending", article.Status);
            Assert.False(article.IsPublished);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
            _mockArticleRepository.Verify(r => r.UpdateAsync(article), Times.Once);
        }

        [Fact]
        public async Task RequestApprovalAsync_WithNonExistentArticle_ShouldReturnFalse()
        {
            // Arrange
            var articleId = 999;
            var staffUserId = 1;

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync((Article?)null);

            // Act
            var result = await _service.RequestApprovalAsync(articleId, staffUserId);

            // Assert
            Assert.False(result);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
            _mockArticleRepository.Verify(r => r.UpdateAsync(It.IsAny<Article>()), Times.Never);
        }

        [Fact]
        public async Task RequestApprovalAsync_WithPublishedStatus_ShouldReturnFalse()
        {
            // Arrange
            var articleId = 1;
            var staffUserId = 1;
            var article = new Article
            {
                ArticleId = articleId,
                Title = "Published Article",
                Summary = "Summary",
                Status = "Published",
                IsPublished = true,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow
            };

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);

            // Act
            var result = await _service.RequestApprovalAsync(articleId, staffUserId);

            // Assert
            Assert.False(result);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
            _mockArticleRepository.Verify(r => r.UpdateAsync(It.IsAny<Article>()), Times.Never);
        }

        [Fact]
        public async Task RequestApprovalAsync_WithPendingStatus_ShouldReturnFalse()
        {
            // Arrange
            var articleId = 1;
            var staffUserId = 1;
            var article = new Article
            {
                ArticleId = articleId,
                Title = "Pending Article",
                Summary = "Summary",
                Status = "Pending",
                IsPublished = false,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow
            };

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);

            // Act
            var result = await _service.RequestApprovalAsync(articleId, staffUserId);

            // Assert
            Assert.False(result);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
            _mockArticleRepository.Verify(r => r.UpdateAsync(It.IsAny<Article>()), Times.Never);
        }
    }
}

