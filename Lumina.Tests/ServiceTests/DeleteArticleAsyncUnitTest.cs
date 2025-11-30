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
    public class DeleteArticleAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly ArticleService _service;

        public DeleteArticleAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockArticleRepository = new Mock<IArticleRepository>();

            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task DeleteArticleAsync_WhenArticleNotFound_ShouldReturnFalse()
        {
            // Arrange
            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync((Article?)null);

            // Act
            var result = await _service.DeleteArticleAsync(1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteArticleAsync_WhenArticleExists_ShouldDeleteAndReturnTrue()
        {
            // Arrange
            var article = new Article { ArticleId = 1, Title = "Test" };

            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article);

            _mockArticleRepository
                .Setup(repo => repo.DeleteAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteArticleAsync(1);

            // Assert
            Assert.True(result);
            _mockArticleRepository.Verify(repo => repo.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteArticleAsync_WhenExceptionOccurs_ShouldReturnFalse()
        {
            // Arrange
            var article = new Article { ArticleId = 1, Title = "Test" };

            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article);

            _mockArticleRepository
                .Setup(repo => repo.DeleteAsync(1))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.DeleteArticleAsync(1);

            // Assert
            Assert.False(result);
        }
    }
}

