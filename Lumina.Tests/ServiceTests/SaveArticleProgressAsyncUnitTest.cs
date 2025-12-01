using Xunit;
using Moq;
using ServiceLayer.Article;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.UserArticleProgress;
using DataLayer.DTOs.Article;
using DataLayer.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class SaveArticleProgressAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IUserArticleProgressRepository> _mockProgressRepository;
        private readonly ArticleService _service;

        public SaveArticleProgressAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockProgressRepository = new Mock<IUserArticleProgressRepository>();

            _mockUnitOfWork.Setup(u => u.UserArticleProgresses).Returns(_mockProgressRepository.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task SaveArticleProgressAsync_WhenInputIsValid_ShouldSaveAndReturnDTO()
        {
            // Arrange
            var request = new ArticleProgressRequestDTO
            {
                ProgressPercent = 50,
                Status = "in_progress"
            };

            var progress = new UserArticleProgress
            {
                ArticleId = 1,
                ProgressPercent = 50,
                Status = "in_progress",
                LastAccessedAt = DateTime.UtcNow,
                CompletedAt = null
            };

            _mockProgressRepository
                .Setup(repo => repo.SaveOrUpdateProgressAsync(1, 1, 50, "in_progress"))
                .ReturnsAsync(progress);

            // Act
            var result = await _service.SaveArticleProgressAsync(1, 1, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ArticleId);
            Assert.Equal(50, result.ProgressPercent);
            Assert.Equal("in_progress", result.Status);
        }

        [Fact]
        public async Task SaveArticleProgressAsync_WhenProgressValuesAreNull_ShouldHandleGracefully()
        {
            // Arrange
            var request = new ArticleProgressRequestDTO
            {
                ProgressPercent = 0,
                Status = "not_started"
            };

            var progress = new UserArticleProgress
            {
                ArticleId = 1,
                ProgressPercent = null,
                Status = null,
                LastAccessedAt = DateTime.UtcNow,
                CompletedAt = null
            };

            _mockProgressRepository
                .Setup(repo => repo.SaveOrUpdateProgressAsync(1, 1, 0, "not_started"))
                .ReturnsAsync(progress);

            // Act
            var result = await _service.SaveArticleProgressAsync(1, 1, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.ProgressPercent);
            Assert.Equal("not_started", result.Status);
        }
    }
}





