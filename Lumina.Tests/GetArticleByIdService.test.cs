using DataLayer.DTOs.Article;
using DataLayer.Models;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.User;
using ServiceLayer.Article;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class GetArticleByIdServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly ArticleService _service;

        public GetArticleByIdServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockArticleRepository = new Mock<IArticleRepository>();
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _mockUserRepository = new Mock<IUserRepository>();

            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);
            _mockUnitOfWork.Setup(u => u.Categories).Returns(_mockCategoryRepository.Object);
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetArticleByIdAsync_WithValidPublishedArticle_ShouldReturnArticleResponseDTO()
        {
            // Arrange
            var articleId = 1;
            var article = new Article
            {
                ArticleId = articleId,
                Title = "Published Article",
                Summary = "Summary",
                IsPublished = true,
                Status = "Published",
                CreatedAt = DateTime.UtcNow,
                CategoryId = 1,
                CreatedBy = 1,
                Category = new ArticleCategory { CategoryId = 1, CategoryName = "Technology" },
                CreatedByNavigation = new User { UserId = 1, FullName = "Author User" },
                ArticleSections = new List<ArticleSection>
                {
                    new ArticleSection
                    {
                        SectionId = 1,
                        ArticleId = articleId,
                        SectionTitle = "Section 1",
                        SectionContent = "Content 1",
                        OrderIndex = 1
                    }
                }
            };

            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Technology" };
            var author = new User { UserId = 1, FullName = "Author User" };

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockCategoryRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(author);

            // Act
            var result = await _service.GetArticleByIdAsync(articleId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(articleId, result.ArticleId);
            Assert.Equal("Published Article", result.Title);
            Assert.Equal("Summary", result.Summary);
            Assert.True(result.IsPublished);
            Assert.Equal("Published", result.Status);
            Assert.Equal("Technology", result.CategoryName);
            Assert.Equal("Author User", result.AuthorName);
            Assert.Single(result.Sections);
            Assert.Equal("Section 1", result.Sections[0].SectionTitle);
        }

        [Fact]
        public async Task GetArticleByIdAsync_WithNonExistentArticle_ShouldReturnNull()
        {
            // Arrange
            var articleId = 999;

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync((Article?)null);

            // Act
            var result = await _service.GetArticleByIdAsync(articleId);

            // Assert
            Assert.Null(result);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
            _mockCategoryRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never);
            _mockUserRepository.Verify(r => r.GetUserByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetArticleByIdAsync_WithUnpublishedArticle_ShouldReturnNull()
        {
            // Arrange
            var articleId = 1;
            var article = new Article
            {
                ArticleId = articleId,
                Title = "Draft Article",
                Summary = "Summary",
                IsPublished = false,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                CategoryId = 1,
                CreatedBy = 1
            };

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);

            // Act
            var result = await _service.GetArticleByIdAsync(articleId);

            // Assert
            Assert.Null(result);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
            _mockCategoryRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never);
            _mockUserRepository.Verify(r => r.GetUserByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetArticleByIdAsync_WithNullIsPublished_ShouldReturnNull()
        {
            // Arrange
            var articleId = 1;
            var article = new Article
            {
                ArticleId = articleId,
                Title = "Article",
                Summary = "Summary",
                IsPublished = null,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                CategoryId = 1,
                CreatedBy = 1
            };

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);

            // Act
            var result = await _service.GetArticleByIdAsync(articleId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetArticleByIdAsync_WithNullCategory_ShouldReturnUnknownCategory()
        {
            // Arrange
            var articleId = 1;
            var article = new Article
            {
                ArticleId = articleId,
                Title = "Published Article",
                Summary = "Summary",
                IsPublished = true,
                Status = "Published",
                CreatedAt = DateTime.UtcNow,
                CategoryId = 999,
                CreatedBy = 1,
                ArticleSections = new List<ArticleSection>()
            };

            var author = new User { UserId = 1, FullName = "Author User" };

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockCategoryRepository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((ArticleCategory?)null);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(author);

            // Act
            var result = await _service.GetArticleByIdAsync(articleId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Unknown", result.CategoryName);
        }

        [Fact]
        public async Task GetArticleByIdAsync_WithNullAuthor_ShouldReturnUnknownAuthor()
        {
            // Arrange
            var articleId = 1;
            var article = new Article
            {
                ArticleId = articleId,
                Title = "Published Article",
                Summary = "Summary",
                IsPublished = true,
                Status = "Published",
                CreatedAt = DateTime.UtcNow,
                CategoryId = 1,
                CreatedBy = 999,
                ArticleSections = new List<ArticleSection>()
            };

            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Technology" };

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockCategoryRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(999)).ReturnsAsync((User?)null);

            // Act
            var result = await _service.GetArticleByIdAsync(articleId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Unknown", result.AuthorName);
        }

        [Fact]
        public async Task GetArticleByIdAsync_WithMultipleSections_ShouldOrderSectionsByOrderIndex()
        {
            // Arrange
            var articleId = 1;
            var article = new Article
            {
                ArticleId = articleId,
                Title = "Published Article",
                Summary = "Summary",
                IsPublished = true,
                Status = "Published",
                CreatedAt = DateTime.UtcNow,
                CategoryId = 1,
                CreatedBy = 1,
                Category = new ArticleCategory { CategoryId = 1, CategoryName = "Technology" },
                CreatedByNavigation = new User { UserId = 1, FullName = "Author User" },
                ArticleSections = new List<ArticleSection>
                {
                    new ArticleSection { SectionId = 3, SectionTitle = "Section 3", SectionContent = "Content 3", OrderIndex = 3 },
                    new ArticleSection { SectionId = 1, SectionTitle = "Section 1", SectionContent = "Content 1", OrderIndex = 1 },
                    new ArticleSection { SectionId = 2, SectionTitle = "Section 2", SectionContent = "Content 2", OrderIndex = 2 }
                }
            };

            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Technology" };
            var author = new User { UserId = 1, FullName = "Author User" };

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockCategoryRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(author);

            // Act
            var result = await _service.GetArticleByIdAsync(articleId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Sections.Count);
            Assert.Equal("Section 1", result.Sections[0].SectionTitle);
            Assert.Equal("Section 2", result.Sections[1].SectionTitle);
            Assert.Equal("Section 3", result.Sections[2].SectionTitle);
        }
    }
}

