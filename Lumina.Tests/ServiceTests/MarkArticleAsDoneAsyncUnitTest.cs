using Xunit;
using Moq;
using ServiceLayer.Article;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.UserArticleProgress;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class MarkArticleAsDoneAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IUserArticleProgressRepository> _mockProgressRepository;
        private readonly ArticleService _service;

        public MarkArticleAsDoneAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockProgressRepository = new Mock<IUserArticleProgressRepository>();

            _mockUnitOfWork.Setup(u => u.UserArticleProgresses).Returns(_mockProgressRepository.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task MarkArticleAsDoneAsync_WhenInputIsValid_ShouldReturnTrue()
        {
            // Arrange
            _mockProgressRepository
                .Setup(repo => repo.MarkArticleAsDoneAsync(1, 1))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.MarkArticleAsDoneAsync(1, 1);

            // Assert
            Assert.True(result);
            _mockProgressRepository.Verify(repo => repo.MarkArticleAsDoneAsync(1, 1), Times.Once);
        }

        [Fact]
        public async Task MarkArticleAsDoneAsync_WhenExceptionOccurs_ShouldReturnFalse()
        {
            // Arrange
            _mockProgressRepository
                .Setup(repo => repo.MarkArticleAsDoneAsync(1, 1))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.MarkArticleAsDoneAsync(1, 1);

            // Assert
            Assert.False(result);
        }
    }
}





