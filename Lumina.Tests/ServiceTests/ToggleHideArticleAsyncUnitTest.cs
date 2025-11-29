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
    public class ToggleHideArticleAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly ArticleService _service;

        public ToggleHideArticleAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockArticleRepository = new Mock<IArticleRepository>();

            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task ToggleHideArticleAsync_WhenArticleNotFound_ShouldReturnFalse()
        {
            // Arrange
            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync((Article?)null);

            // Act
            var result = await _service.ToggleHideArticleAsync(1, false, 1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ToggleHideArticleAsync_WhenStatusIsNotPublished_ShouldReturnFalse()
        {
            // Arrange
            var article = new Article
            {
                ArticleId = 1,
                Status = "Draft"
            };

            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article);

            // Act
            var result = await _service.ToggleHideArticleAsync(1, false, 1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ToggleHideArticleAsync_WhenInputIsValid_ShouldUpdateAndReturnTrue()
        {
            // Arrange
            var article = new Article
            {
                ArticleId = 1,
                Status = "Published",
                IsPublished = true
            };

            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article);

            _mockArticleRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Article>()))
                .ReturnsAsync((Article a) => a);

            // Act
            var result = await _service.ToggleHideArticleAsync(1, false, 1);

            // Assert
            Assert.True(result);
            Assert.False(article.IsPublished);
            Assert.Equal(1, article.UpdatedBy);
            Assert.NotNull(article.UpdatedAt);
        }
    }
}

