using Xunit;
using Moq;
using ServiceLayer.Article;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer;
using DataLayer.DTOs.Article;
using DataLayer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class CreateArticleAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly Mock<RepositoryLayer.User.IUserRepository> _mockUserRepository;
        private readonly Mock<IDbContextTransaction> _mockTransaction;
        private readonly ArticleService _service;

        public CreateArticleAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockArticleRepository = new Mock<IArticleRepository>();
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _mockUserRepository = new Mock<RepositoryLayer.User.IUserRepository>();
            _mockTransaction = new Mock<IDbContextTransaction>();

            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);
            _mockUnitOfWork.Setup(u => u.Categories).Returns(_mockCategoryRepository.Object);
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_mockTransaction.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateArticleAsync_WhenCategoryNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "Test Summary",
                CategoryId = 1
            };

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync((ArticleCategory?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _service.CreateArticleAsync(request, 1)
            );
        }

        [Fact]
        public async Task CreateArticleAsync_WhenCreatorNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "Test Summary",
                CategoryId = 1
            };

            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Test" };

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(category);

            _mockUserRepository
                .Setup(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _service.CreateArticleAsync(request, 1)
            );
        }

        [Fact]
        public async Task CreateArticleAsync_WhenInputIsValid_ShouldCreateAndReturnDTO()
        {
            // Arrange
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "Test Summary",
                CategoryId = 1,
                PublishNow = false,
                Sections = new List<ArticleSectionCreateDTO>
                {
                    new ArticleSectionCreateDTO
                    {
                        SectionTitle = "Section 1",
                        SectionContent = "Content 1",
                        OrderIndex = 1
                    }
                }
            };

            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Test" };
            var creator = new User { UserId = 1, FullName = "Test User" };

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(category);

            _mockUserRepository
                .Setup(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync(creator);

            Article? capturedArticle = null;
            _mockArticleRepository
                .Setup(repo => repo.AddAsync(It.IsAny<Article>()))
                .Callback<Article>(a => capturedArticle = a)
                .Returns(Task.CompletedTask);

            _mockArticleRepository
                .Setup(repo => repo.AddSectionsRangeAsync(It.IsAny<IEnumerable<ArticleSection>>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            _mockTransaction
                .Setup(t => t.CommitAsync(It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateArticleAsync(request, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Article", result.Title);
            Assert.Equal("Draft", result.Status);
            Assert.False(result.IsPublished);
            Assert.NotNull(capturedArticle);
            Assert.Equal("Draft", capturedArticle.Status);
        }

        [Fact]
        public async Task CreateArticleAsync_WhenPublishNowIsTrue_ShouldSetStatusToPublished()
        {
            // Arrange
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "Test Summary",
                CategoryId = 1,
                PublishNow = true,
                Sections = null
            };

            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Test" };
            var creator = new User { UserId = 1, FullName = "Test User" };

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(category);

            _mockUserRepository
                .Setup(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync(creator);

            _mockArticleRepository
                .Setup(repo => repo.AddAsync(It.IsAny<Article>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            _mockTransaction
                .Setup(t => t.CommitAsync(It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateArticleAsync(request, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Published", result.Status);
            Assert.True(result.IsPublished);
        }

        [Fact]
        public async Task CreateArticleAsync_WhenExceptionOccurs_ShouldRollbackTransaction()
        {
            // Arrange
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "Test Summary",
                CategoryId = 1
            };

            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Test" };
            var creator = new User { UserId = 1, FullName = "Test User" };

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(category);

            _mockUserRepository
                .Setup(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync(creator);

            _mockArticleRepository
                .Setup(repo => repo.AddAsync(It.IsAny<Article>()))
                .ThrowsAsync(new Exception("Database error"));

            _mockTransaction
                .Setup(t => t.RollbackAsync(It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _service.CreateArticleAsync(request, 1)
            );

            _mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }
    }
}

