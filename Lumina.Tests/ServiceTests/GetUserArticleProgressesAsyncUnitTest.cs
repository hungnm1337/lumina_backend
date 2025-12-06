using Xunit;
using Moq;
using ServiceLayer.Article;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.UserArticleProgress;
using DataLayer.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class GetUserArticleProgressesAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IUserArticleProgressRepository> _mockProgressRepository;
        private readonly ArticleService _service;

        public GetUserArticleProgressesAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockProgressRepository = new Mock<IUserArticleProgressRepository>();

            _mockUnitOfWork.Setup(u => u.UserArticleProgresses).Returns(_mockProgressRepository.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetUserArticleProgressesAsync_WhenInputIsValid_ShouldReturnProgresses()
        {
            // Arrange
            var articleIds = new List<int> { 1, 2 };

            var progresses = new List<UserArticleProgress>
            {
                new UserArticleProgress
                {
                    ArticleId = 1,
                    ProgressPercent = 50,
                    Status = "in_progress",
                    LastAccessedAt = DateTime.UtcNow,
                    CompletedAt = null
                },
                new UserArticleProgress
                {
                    ArticleId = 2,
                    ProgressPercent = null,
                    Status = null,
                    LastAccessedAt = DateTime.UtcNow,
                    CompletedAt = null
                }
            };

            _mockProgressRepository
                .Setup(repo => repo.GetUserArticleProgressesAsync(1, articleIds))
                .ReturnsAsync(progresses);

            // Act
            var result = await _service.GetUserArticleProgressesAsync(1, articleIds);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(50, result[0].ProgressPercent);
            Assert.Equal(0, result[1].ProgressPercent);
            Assert.Equal("not_started", result[1].Status);
        }

        [Fact]
        public async Task GetUserArticleProgressesAsync_WhenNoProgresses_ShouldReturnEmptyList()
        {
            // Arrange
            var articleIds = new List<int> { 1, 2 };

            _mockProgressRepository
                .Setup(repo => repo.GetUserArticleProgressesAsync(1, articleIds))
                .ReturnsAsync(new List<UserArticleProgress>());

            // Act
            var result = await _service.GetUserArticleProgressesAsync(1, articleIds);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}













