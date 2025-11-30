using Xunit;
using Moq;
using ServiceLayer.Article;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer;
using DataLayer.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    /// <summary>
    /// Test cases for ArticleService.RequestApprovalAsync method
    /// Following AAA (Arrange-Act-Assert) pattern with Moq verification
    /// </summary>
    public class ArticleRequestApprovalAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly ArticleService _service;

        public ArticleRequestApprovalAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockArticleRepository = new Mock<IArticleRepository>();
            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);
            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        #region ID Validation Tests

        [Fact]
        public async Task RequestApprovalAsync_WhenArticleNotFound_ShouldReturnFalse()
        {
            // Arrange
            int articleId = 999;
            int staffUserId = 1;
            _mockArticleRepository.Setup(repo => repo.FindByIdAsync(articleId)).ReturnsAsync((Article?)null);

            // Act
            var result = await _service.RequestApprovalAsync(articleId, staffUserId);

            // Assert
            Assert.False(result);
            _mockArticleRepository.Verify(repo => repo.FindByIdAsync(articleId), Times.Once);
            _mockArticleRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Article>()), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-999)]
        public async Task RequestApprovalAsync_WhenArticleIdIsInvalidBoundary_ShouldReturnFalse(int invalidId)
        {
            // Arrange
            _mockArticleRepository.Setup(repo => repo.FindByIdAsync(invalidId)).ReturnsAsync((Article?)null);

            // Act
            var result = await _service.RequestApprovalAsync(invalidId, 1);

            // Assert
            Assert.False(result);
            _mockArticleRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Article>()), Times.Never);
        }

        #endregion

        #region Status Validation Tests

        [Theory]
        [InlineData("Published")]
        [InlineData("Pending")]
        [InlineData("SomeOtherStatus")]
        public async Task RequestApprovalAsync_WhenStatusIsNotDraftOrRejected_ShouldReturnFalse(string invalidStatus)
        {
            // Arrange
            var article = new Article
            {
                ArticleId = 1,
                Title = "Test Article",
                Status = invalidStatus,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow
            };
            _mockArticleRepository.Setup(repo => repo.FindByIdAsync(1)).ReturnsAsync(article);

            // Act
            var result = await _service.RequestApprovalAsync(1, 1);

            // Assert
            Assert.False(result);
            _mockArticleRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Article>()), Times.Never);
        }

        #endregion

        #region Valid Success Tests

        [Theory]
        [InlineData("Draft")]
        [InlineData("Rejected")]
        public async Task RequestApprovalAsync_WhenStatusIsValid_ShouldUpdateToPendingAndReturnTrue(string validStatus)
        {
            // Arrange
            int articleId = 1;
            int staffUserId = 5;
            var article = new Article
            {
                ArticleId = articleId,
                Title = "Test Article",
                Status = validStatus,
                IsPublished = false,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow
            };

            _mockArticleRepository.Setup(repo => repo.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockArticleRepository.Setup(repo => repo.UpdateAsync(It.IsAny<Article>())).ReturnsAsync((Article a) => a);

            // Act
            var result = await _service.RequestApprovalAsync(articleId, staffUserId);

            // Assert
            Assert.True(result);
            Assert.Equal("Pending", article.Status);
            Assert.False(article.IsPublished);
            Assert.Equal(staffUserId, article.UpdatedBy);
            Assert.NotEqual(default(DateTime), article.UpdatedAt);

            // Verify repository calls
            _mockArticleRepository.Verify(repo => repo.FindByIdAsync(articleId), Times.Once);
            _mockArticleRepository.Verify(
                repo => repo.UpdateAsync(It.Is<Article>(a =>
                    a.Status == "Pending" &&
                    a.IsPublished == false &&
                    a.UpdatedBy == staffUserId)),
                Times.Once);
        }

        [Fact]
        public async Task RequestApprovalAsync_WhenCalled_ShouldSetUpdatedAtToCurrentTime()
        {
            // Arrange
            var beforeTime = DateTime.UtcNow;
            var article = new Article
            {
                ArticleId = 1,
                Title = "Test",
                Status = "Draft",
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
            _mockArticleRepository.Setup(repo => repo.FindByIdAsync(1)).ReturnsAsync(article);
            _mockArticleRepository.Setup(repo => repo.UpdateAsync(It.IsAny<Article>())).ReturnsAsync((Article a) => a);

            // Act
            await _service.RequestApprovalAsync(1, 1);
            var afterTime = DateTime.UtcNow;

            // Assert
            Assert.True(article.UpdatedAt >= beforeTime && article.UpdatedAt <= afterTime);
        }

        [Fact]
        public async Task RequestApprovalAsync_WhenValidRequest_ShouldSetIsPublishedToFalse()
        {
            // Arrange
            var article = new Article
            {
                ArticleId = 1,
                Title = "Test",
                Status = "Draft",
                IsPublished = true, // Starting as true
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow
            };
            _mockArticleRepository.Setup(repo => repo.FindByIdAsync(1)).ReturnsAsync(article);
            _mockArticleRepository.Setup(repo => repo.UpdateAsync(It.IsAny<Article>())).ReturnsAsync((Article a) => a);

            // Act
            await _service.RequestApprovalAsync(1, 1);

            // Assert
            Assert.False(article.IsPublished);
        }

        [Fact]
        public async Task RequestApprovalAsync_WhenRejectedArticleResubmitted_ShouldUpdateCorrectly()
        {
            // Arrange - Article bị reject trước đó và được gửi lại
            var article = new Article
            {
                ArticleId = 1,
                Title = "Rejected Article",
                Status = "Rejected",
                IsPublished = false,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                RejectionReason = "Previous rejection"
            };

            _mockArticleRepository.Setup(repo => repo.FindByIdAsync(1)).ReturnsAsync(article);
            _mockArticleRepository.Setup(repo => repo.UpdateAsync(It.IsAny<Article>())).ReturnsAsync((Article a) => a);

            // Act
            var result = await _service.RequestApprovalAsync(1, 3);

            // Assert
            Assert.True(result);
            Assert.Equal("Pending", article.Status);
            Assert.Equal(3, article.UpdatedBy);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task RequestApprovalAsync_WhenStaffUserIdIsZero_ShouldStillProcess()
        {
            // Arrange
            var article = new Article
            {
                ArticleId = 1,
                Title = "Test",
                Status = "Draft",
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow
            };
            _mockArticleRepository.Setup(repo => repo.FindByIdAsync(1)).ReturnsAsync(article);
            _mockArticleRepository.Setup(repo => repo.UpdateAsync(It.IsAny<Article>())).ReturnsAsync((Article a) => a);

            // Act
            var result = await _service.RequestApprovalAsync(1, 0);

            // Assert
            Assert.True(result);
            Assert.Equal(0, article.UpdatedBy);
        }

        #endregion
    }
}
