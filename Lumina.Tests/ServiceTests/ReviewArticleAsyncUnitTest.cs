using Xunit;
using Moq;
using ServiceLayer.Article;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer;
using DataLayer.DTOs.Article;
using DataLayer.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class ReviewArticleAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly ArticleService _service;

        public ReviewArticleAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockArticleRepository = new Mock<IArticleRepository>();

            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task ReviewArticleAsync_WhenArticleNotFound_ShouldReturnFalse()
        {
            // Arrange
            var request = new ArticleReviewRequest
            {
                IsApproved = true
            };

            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync((Article?)null);

            // Act
            var result = await _service.ReviewArticleAsync(1, request, 1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ReviewArticleAsync_WhenStatusIsNotPending_ShouldReturnFalse()
        {
            // Arrange
            var request = new ArticleReviewRequest
            {
                IsApproved = true
            };

            var article = new Article
            {
                ArticleId = 1,
                Status = "Draft"
            };

            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article);

            // Act
            var result = await _service.ReviewArticleAsync(1, request, 1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ReviewArticleAsync_WhenApproved_ShouldUpdateToPublished()
        {
            // Arrange
            var request = new ArticleReviewRequest
            {
                IsApproved = true,
                Comment = null
            };

            var article = new Article
            {
                ArticleId = 1,
                Status = "Pending",
                IsPublished = false,
                RejectionReason = "Previous rejection"
            };

            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article);

            _mockArticleRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Article>()))
                .ReturnsAsync((Article a) => a);

            // Act
            var result = await _service.ReviewArticleAsync(1, request, 1);

            // Assert
            Assert.True(result);
            Assert.Equal("Published", article.Status);
            Assert.True(article.IsPublished);
            Assert.Null(article.RejectionReason);
            Assert.Equal(1, article.UpdatedBy);
            Assert.NotNull(article.UpdatedAt);
        }

        [Fact]
        public async Task ReviewArticleAsync_WhenRejected_ShouldUpdateToRejected()
        {
            // Arrange
            var request = new ArticleReviewRequest
            {
                IsApproved = false,
                Comment = "Not suitable"
            };

            var article = new Article
            {
                ArticleId = 1,
                Status = "Pending",
                IsPublished = false
            };

            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article);

            _mockArticleRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Article>()))
                .ReturnsAsync((Article a) => a);

            // Act
            var result = await _service.ReviewArticleAsync(1, request, 1);

            // Assert
            Assert.True(result);
            Assert.Equal("Rejected", article.Status);
            Assert.False(article.IsPublished);
            Assert.Equal("Not suitable", article.RejectionReason);
            Assert.Equal(1, article.UpdatedBy);
            Assert.NotNull(article.UpdatedAt);
        }
    }
}

