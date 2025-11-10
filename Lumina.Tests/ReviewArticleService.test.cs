using DataLayer.DTOs.Article;
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
    public class ReviewArticleServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly ArticleService _service;

        public ReviewArticleServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockArticleRepository = new Mock<IArticleRepository>();

            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task ReviewArticleAsync_WithApproval_ShouldSetStatusToPublished()
        {
            // Arrange
            var articleId = 1;
            var managerUserId = 2;
            var request = new ArticleReviewRequest
            {
                IsApproved = true,
                Comment = "Good article"
            };

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
            _mockArticleRepository.Setup(r => r.UpdateAsync(It.IsAny<Article>())).ReturnsAsync(article);

            // Act
            var result = await _service.ReviewArticleAsync(articleId, request, managerUserId);

            // Assert
            Assert.True(result);
            Assert.Equal("Published", article.Status);
            Assert.True(article.IsPublished);
            Assert.Null(article.RejectionReason);
            Assert.Equal(managerUserId, article.UpdatedBy);
            Assert.NotNull(article.UpdatedAt);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
            _mockArticleRepository.Verify(r => r.UpdateAsync(article), Times.Once);
        }

        [Fact]
        public async Task ReviewArticleAsync_WithRejection_ShouldSetStatusToRejected()
        {
            // Arrange
            var articleId = 1;
            var managerUserId = 2;
            var request = new ArticleReviewRequest
            {
                IsApproved = false,
                Comment = "Needs improvement"
            };

            var article = new Article
            {
                ArticleId = articleId,
                Title = "Pending Article",
                Summary = "Summary",
                Status = "Pending",
                IsPublished = false,
                RejectionReason = null,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow
            };

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockArticleRepository.Setup(r => r.UpdateAsync(It.IsAny<Article>())).ReturnsAsync(article);

            // Act
            var result = await _service.ReviewArticleAsync(articleId, request, managerUserId);

            // Assert
            Assert.True(result);
            Assert.Equal("Rejected", article.Status);
            Assert.False(article.IsPublished);
            Assert.Equal("Needs improvement", article.RejectionReason);
            Assert.Equal(managerUserId, article.UpdatedBy);
            Assert.NotNull(article.UpdatedAt);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
            _mockArticleRepository.Verify(r => r.UpdateAsync(article), Times.Once);
        }

        [Fact]
        public async Task ReviewArticleAsync_WithApprovalAndExistingRejectionReason_ShouldClearRejectionReason()
        {
            // Arrange
            var articleId = 1;
            var managerUserId = 2;
            var request = new ArticleReviewRequest
            {
                IsApproved = true,
                Comment = "Good article"
            };

            var article = new Article
            {
                ArticleId = articleId,
                Title = "Pending Article",
                Summary = "Summary",
                Status = "Pending",
                IsPublished = false,
                RejectionReason = "Previous rejection",
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow
            };

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockArticleRepository.Setup(r => r.UpdateAsync(It.IsAny<Article>())).ReturnsAsync(article);

            // Act
            var result = await _service.ReviewArticleAsync(articleId, request, managerUserId);

            // Assert
            Assert.True(result);
            Assert.Equal("Published", article.Status);
            Assert.True(article.IsPublished);
            Assert.Null(article.RejectionReason);
        }

        [Fact]
        public async Task ReviewArticleAsync_WithNonExistentArticle_ShouldReturnFalse()
        {
            // Arrange
            var articleId = 999;
            var managerUserId = 2;
            var request = new ArticleReviewRequest
            {
                IsApproved = true,
                Comment = "Good article"
            };

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync((Article?)null);

            // Act
            var result = await _service.ReviewArticleAsync(articleId, request, managerUserId);

            // Assert
            Assert.False(result);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
            _mockArticleRepository.Verify(r => r.UpdateAsync(It.IsAny<Article>()), Times.Never);
        }

        [Fact]
        public async Task ReviewArticleAsync_WithNonPendingStatus_ShouldReturnFalse()
        {
            // Arrange
            var articleId = 1;
            var managerUserId = 2;
            var request = new ArticleReviewRequest
            {
                IsApproved = true,
                Comment = "Good article"
            };

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
            var result = await _service.ReviewArticleAsync(articleId, request, managerUserId);

            // Assert
            Assert.False(result);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
            _mockArticleRepository.Verify(r => r.UpdateAsync(It.IsAny<Article>()), Times.Never);
        }

        [Fact]
        public async Task ReviewArticleAsync_WithDraftStatus_ShouldReturnFalse()
        {
            // Arrange
            var articleId = 1;
            var managerUserId = 2;
            var request = new ArticleReviewRequest
            {
                IsApproved = true,
                Comment = "Good article"
            };

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

            // Act
            var result = await _service.ReviewArticleAsync(articleId, request, managerUserId);

            // Assert
            Assert.False(result);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
            _mockArticleRepository.Verify(r => r.UpdateAsync(It.IsAny<Article>()), Times.Never);
        }
    }
}

