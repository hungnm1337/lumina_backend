using DataLayer.DTOs.Article;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.User;
using ServiceLayer.Article;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class CreateArticleServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IDbContextTransaction> _mockTransaction;
        private readonly ArticleService _service;

        public CreateArticleServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockArticleRepository = new Mock<IArticleRepository>();
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockTransaction = new Mock<IDbContextTransaction>();

            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);
            _mockUnitOfWork.Setup(u => u.Categories).Returns(_mockCategoryRepository.Object);
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateArticleAsync_WithValidData_ShouldReturnArticleResponseDTO()
        {
            // Arrange
            var creatorUserId = 1;
            var categoryId = 1;
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "Test Summary",
                CategoryId = categoryId,
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

            var category = new ArticleCategory
            {
                CategoryId = categoryId,
                CategoryName = "Technology"
            };

            var creator = new User
            {
                UserId = creatorUserId,
                FullName = "Test User"
            };

            _mockCategoryRepository.Setup(r => r.FindByIdAsync(categoryId)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(creatorUserId)).ReturnsAsync(creator);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_mockTransaction.Object);
            _mockArticleRepository.Setup(r => r.AddAsync(It.IsAny<Article>())).Returns(Task.CompletedTask);
            _mockArticleRepository.Setup(r => r.AddSectionsRangeAsync(It.IsAny<IEnumerable<ArticleSection>>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            _mockTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateArticleAsync(request, creatorUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Article", result.Title);
            Assert.Equal("Test Summary", result.Summary);
            Assert.False(result.IsPublished);
            Assert.Equal("Draft", result.Status);
            Assert.Equal("Test User", result.AuthorName);
            Assert.Equal("Technology", result.CategoryName);
            Assert.Single(result.Sections);
            Assert.Equal("Section 1", result.Sections[0].SectionTitle);

            _mockCategoryRepository.Verify(r => r.FindByIdAsync(categoryId), Times.Once);
            _mockUserRepository.Verify(r => r.GetUserByIdAsync(creatorUserId), Times.Once);
            _mockArticleRepository.Verify(r => r.AddAsync(It.IsAny<Article>()), Times.Once);
            _mockArticleRepository.Verify(r => r.AddSectionsRangeAsync(It.IsAny<IEnumerable<ArticleSection>>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Exactly(2));
            _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateArticleAsync_WithPublishNowTrue_ShouldSetStatusToPublished()
        {
            // Arrange
            var creatorUserId = 1;
            var categoryId = 1;
            var request = new ArticleCreateDTO
            {
                Title = "Published Article",
                Summary = "Published Summary",
                CategoryId = categoryId,
                PublishNow = true,
                Sections = new List<ArticleSectionCreateDTO>()
            };

            var category = new ArticleCategory { CategoryId = categoryId, CategoryName = "Technology" };
            var creator = new User { UserId = creatorUserId, FullName = "Test User" };

            _mockCategoryRepository.Setup(r => r.FindByIdAsync(categoryId)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(creatorUserId)).ReturnsAsync(creator);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_mockTransaction.Object);
            _mockArticleRepository.Setup(r => r.AddAsync(It.IsAny<Article>()))
                .Returns<Article>(article =>
                {
                    article.ArticleId = 1;
                    return Task.CompletedTask;
                });
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            _mockTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateArticleAsync(request, creatorUserId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsPublished);
            Assert.Equal("Published", result.Status);
        }

        [Fact]
        public async Task CreateArticleAsync_WithoutSections_ShouldReturnArticleWithoutSections()
        {
            // Arrange
            var creatorUserId = 1;
            var categoryId = 1;
            var request = new ArticleCreateDTO
            {
                Title = "Article Without Sections",
                Summary = "Summary",
                CategoryId = categoryId,
                PublishNow = false,
                Sections = null
            };

            var category = new ArticleCategory { CategoryId = categoryId, CategoryName = "Technology" };
            var creator = new User { UserId = creatorUserId, FullName = "Test User" };

            _mockCategoryRepository.Setup(r => r.FindByIdAsync(categoryId)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(creatorUserId)).ReturnsAsync(creator);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_mockTransaction.Object);
            _mockArticleRepository.Setup(r => r.AddAsync(It.IsAny<Article>()))
                .Returns<Article>(article =>
                {
                    article.ArticleId = 1;
                    return Task.CompletedTask;
                });
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            _mockTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateArticleAsync(request, creatorUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Sections);
            _mockArticleRepository.Verify(r => r.AddSectionsRangeAsync(It.IsAny<IEnumerable<ArticleSection>>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once); // Only once for article, not for sections
        }

        [Fact]
        public async Task CreateArticleAsync_WithEmptySections_ShouldReturnArticleWithoutSections()
        {
            // Arrange
            var creatorUserId = 1;
            var categoryId = 1;
            var request = new ArticleCreateDTO
            {
                Title = "Article With Empty Sections",
                Summary = "Summary",
                CategoryId = categoryId,
                PublishNow = false,
                Sections = new List<ArticleSectionCreateDTO>()
            };

            var category = new ArticleCategory { CategoryId = categoryId, CategoryName = "Technology" };
            var creator = new User { UserId = creatorUserId, FullName = "Test User" };

            _mockCategoryRepository.Setup(r => r.FindByIdAsync(categoryId)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(creatorUserId)).ReturnsAsync(creator);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_mockTransaction.Object);
            _mockArticleRepository.Setup(r => r.AddAsync(It.IsAny<Article>()))
                .Returns<Article>(article =>
                {
                    article.ArticleId = 1;
                    return Task.CompletedTask;
                });
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            _mockTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateArticleAsync(request, creatorUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Sections);
            _mockArticleRepository.Verify(r => r.AddSectionsRangeAsync(It.IsAny<IEnumerable<ArticleSection>>()), Times.Never);
        }

        [Fact]
        public async Task CreateArticleAsync_WithCategoryNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var creatorUserId = 1;
            var categoryId = 999;
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "Test Summary",
                CategoryId = categoryId,
                PublishNow = false
            };

            _mockCategoryRepository.Setup(r => r.FindByIdAsync(categoryId)).ReturnsAsync((ArticleCategory?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await _service.CreateArticleAsync(request, creatorUserId);
            });

            Assert.Equal("Category not found.", exception.Message);
            _mockCategoryRepository.Verify(r => r.FindByIdAsync(categoryId), Times.Once);
            _mockUserRepository.Verify(r => r.GetUserByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task CreateArticleAsync_WithUserNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var creatorUserId = 999;
            var categoryId = 1;
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "Test Summary",
                CategoryId = categoryId,
                PublishNow = false
            };

            var category = new ArticleCategory { CategoryId = categoryId, CategoryName = "Technology" };

            _mockCategoryRepository.Setup(r => r.FindByIdAsync(categoryId)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(creatorUserId)).ReturnsAsync((User?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await _service.CreateArticleAsync(request, creatorUserId);
            });

            Assert.Equal("Creator user not found.", exception.Message);
            _mockCategoryRepository.Verify(r => r.FindByIdAsync(categoryId), Times.Once);
            _mockUserRepository.Verify(r => r.GetUserByIdAsync(creatorUserId), Times.Once);
        }

        [Fact]
        public async Task CreateArticleAsync_WhenExceptionOccurs_ShouldRollbackTransaction()
        {
            // Arrange
            var creatorUserId = 1;
            var categoryId = 1;
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "Test Summary",
                CategoryId = categoryId,
                PublishNow = false
            };

            var category = new ArticleCategory { CategoryId = categoryId, CategoryName = "Technology" };
            var creator = new User { UserId = creatorUserId, FullName = "Test User" };

            _mockCategoryRepository.Setup(r => r.FindByIdAsync(categoryId)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(creatorUserId)).ReturnsAsync(creator);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_mockTransaction.Object);
            _mockArticleRepository.Setup(r => r.AddAsync(It.IsAny<Article>())).ThrowsAsync(new Exception("Database error"));
            _mockTransaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _service.CreateArticleAsync(request, creatorUserId);
            });

            _mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateArticleAsync_WithMultipleSections_ShouldOrderSectionsByOrderIndex()
        {
            // Arrange
            var creatorUserId = 1;
            var categoryId = 1;
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "Test Summary",
                CategoryId = categoryId,
                PublishNow = false,
                Sections = new List<ArticleSectionCreateDTO>
                {
                    new ArticleSectionCreateDTO { SectionTitle = "Section 3", SectionContent = "Content 3", OrderIndex = 3 },
                    new ArticleSectionCreateDTO { SectionTitle = "Section 1", SectionContent = "Content 1", OrderIndex = 1 },
                    new ArticleSectionCreateDTO { SectionTitle = "Section 2", SectionContent = "Content 2", OrderIndex = 2 }
                }
            };

            var category = new ArticleCategory { CategoryId = categoryId, CategoryName = "Technology" };
            var creator = new User { UserId = creatorUserId, FullName = "Test User" };

            _mockCategoryRepository.Setup(r => r.FindByIdAsync(categoryId)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(creatorUserId)).ReturnsAsync(creator);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_mockTransaction.Object);
            _mockArticleRepository.Setup(r => r.AddAsync(It.IsAny<Article>()))
                .Returns<Article>(article =>
                {
                    article.ArticleId = 1;
                    return Task.CompletedTask;
                });
            _mockArticleRepository.Setup(r => r.AddSectionsRangeAsync(It.IsAny<IEnumerable<ArticleSection>>()))
                .Returns<IEnumerable<ArticleSection>>(sections =>
                {
                    int sectionId = 1;
                    foreach (var section in sections)
                    {
                        section.SectionId = sectionId++;
                    }
                    return Task.CompletedTask;
                });
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            _mockTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateArticleAsync(request, creatorUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Sections.Count);
            Assert.Equal("Section 1", result.Sections[0].SectionTitle);
            Assert.Equal("Section 2", result.Sections[1].SectionTitle);
            Assert.Equal("Section 3", result.Sections[2].SectionTitle);
        }

        [Fact]
        public async Task CreateArticleAsync_WhenCommitThrowsException_ShouldRollbackTransaction()
        {
            // Arrange
            var creatorUserId = 1;
            var categoryId = 1;
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "Test Summary",
                CategoryId = categoryId,
                PublishNow = false
            };

            var category = new ArticleCategory { CategoryId = categoryId, CategoryName = "Technology" };
            var creator = new User { UserId = creatorUserId, FullName = "Test User" };

            _mockCategoryRepository.Setup(r => r.FindByIdAsync(categoryId)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(creatorUserId)).ReturnsAsync(creator);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_mockTransaction.Object);
            _mockArticleRepository.Setup(r => r.AddAsync(It.IsAny<Article>()))
                .Returns<Article>(article =>
                {
                    article.ArticleId = 1;
                    return Task.CompletedTask;
                });
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            _mockTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Commit error"));
            _mockTransaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _service.CreateArticleAsync(request, creatorUserId);
            });

            _mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateArticleAsync_WhenAddSectionsThrowsException_ShouldRollbackTransaction()
        {
            // Arrange
            var creatorUserId = 1;
            var categoryId = 1;
            var request = new ArticleCreateDTO
            {
                Title = "Test Article",
                Summary = "Test Summary",
                CategoryId = categoryId,
                PublishNow = false,
                Sections = new List<ArticleSectionCreateDTO>
                {
                    new ArticleSectionCreateDTO { SectionTitle = "Section 1", SectionContent = "Content 1", OrderIndex = 1 }
                }
            };

            var category = new ArticleCategory { CategoryId = categoryId, CategoryName = "Technology" };
            var creator = new User { UserId = creatorUserId, FullName = "Test User" };

            _mockCategoryRepository.Setup(r => r.FindByIdAsync(categoryId)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(creatorUserId)).ReturnsAsync(creator);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_mockTransaction.Object);
            _mockArticleRepository.Setup(r => r.AddAsync(It.IsAny<Article>()))
                .Returns<Article>(article =>
                {
                    article.ArticleId = 1;
                    return Task.CompletedTask;
                });
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            _mockArticleRepository.Setup(r => r.AddSectionsRangeAsync(It.IsAny<IEnumerable<ArticleSection>>()))
                .ThrowsAsync(new Exception("Add sections error"));
            _mockTransaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _service.CreateArticleAsync(request, creatorUserId);
            });

            _mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}

