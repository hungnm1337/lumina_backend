using Xunit;
using Moq;
using ServiceLayer.Article;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer;
using DataLayer.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class GetArticleByIdForManagerAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly Mock<RepositoryLayer.User.IUserRepository> _mockUserRepository;
        private readonly ArticleService _service;

        public GetArticleByIdForManagerAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockArticleRepository = new Mock<IArticleRepository>();
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _mockUserRepository = new Mock<RepositoryLayer.User.IUserRepository>();

            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);
            _mockUnitOfWork.Setup(u => u.Categories).Returns(_mockCategoryRepository.Object);
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetArticleByIdForManagerAsync_WhenArticleNotFound_ShouldReturnNull()
        {
            // Arrange
            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync((Article?)null);

            // Act
            var result = await _service.GetArticleByIdForManagerAsync(1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetArticleByIdForManagerAsync_WhenArticleExists_ShouldReturnDTO()
        {
            // Arrange
            var article = new Article
            {
                ArticleId = 1,
                Title = "Test",
                Summary = "Summary",
                Status = "Draft",
                IsPublished = false,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                ArticleSections = new List<ArticleSection>()
            };

            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Test" };
            var author = new User { UserId = 1, FullName = "Author" };

            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article);

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(category);

            _mockUserRepository
                .Setup(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync(author);

            // Act
            var result = await _service.GetArticleByIdForManagerAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ArticleId);
            Assert.Equal("Test", result.Title);
        }

        [Fact]
        public async Task GetArticleByIdForManagerAsync_WhenCategoryIsNull_ShouldReturnDTOWithUnknownCategory()
        {
            // Arrange
            var article = new Article
            {
                ArticleId = 1,
                Title = "Test",
                Summary = "Summary",
                Status = "Draft",
                IsPublished = false,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                ArticleSections = new List<ArticleSection>()
            };

            var author = new User { UserId = 1, FullName = "Author" };

            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article);

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync((ArticleCategory?)null); // Category is null

            _mockUserRepository
                .Setup(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync(author);

            // Act
            var result = await _service.GetArticleByIdForManagerAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Unknown", result.CategoryName);
        }

        [Fact]
        public async Task GetArticleByIdForManagerAsync_WhenAuthorIsNull_ShouldReturnDTOWithUnknownAuthor()
        {
            // Arrange
            var article = new Article
            {
                ArticleId = 1,
                Title = "Test",
                Summary = "Summary",
                Status = "Draft",
                IsPublished = false,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                ArticleSections = new List<ArticleSection>()
            };

            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Test" };

            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article);

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(category);

            _mockUserRepository
                .Setup(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync((User?)null); // Author is null

            // Act
            var result = await _service.GetArticleByIdForManagerAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Unknown", result.AuthorName);
        }

        [Fact]
        public async Task GetArticleByIdForManagerAsync_WhenArticleSectionsIsEmpty_ShouldReturnEmptySections()
        {
            // Arrange
            var article = new Article
            {
                ArticleId = 1,
                Title = "Test",
                Summary = "Summary",
                Status = "Draft",
                IsPublished = false,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                ArticleSections = new List<ArticleSection>() // Empty list
            };

            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Test" };
            var author = new User { UserId = 1, FullName = "Author" };

            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article);

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(category);

            _mockUserRepository
                .Setup(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync(author);

            // Act
            var result = await _service.GetArticleByIdForManagerAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Sections);
        }

        [Fact]
        public async Task GetArticleByIdForManagerAsync_WhenArticleHasSections_ShouldReturnOrderedSections()
        {
            // Arrange
            var article = new Article
            {
                ArticleId = 1,
                Title = "Test",
                Summary = "Summary",
                Status = "Draft",
                IsPublished = false,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                ArticleSections = new List<ArticleSection>
                {
                    new ArticleSection
                    {
                        SectionId = 1,
                        SectionTitle = "Section 3",
                        SectionContent = "Content 3",
                        OrderIndex = 3
                    },
                    new ArticleSection
                    {
                        SectionId = 2,
                        SectionTitle = "Section 1",
                        SectionContent = "Content 1",
                        OrderIndex = 1
                    },
                    new ArticleSection
                    {
                        SectionId = 3,
                        SectionTitle = "Section 2",
                        SectionContent = "Content 2",
                        OrderIndex = 2
                    }
                }
            };

            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Test" };
            var author = new User { UserId = 1, FullName = "Author" };

            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article);

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(category);

            _mockUserRepository
                .Setup(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync(author);

            // Act
            var result = await _service.GetArticleByIdForManagerAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Sections.Count);
            // Verify sections are ordered by OrderIndex
            Assert.Equal(1, result.Sections[0].OrderIndex);
            Assert.Equal("Section 1", result.Sections[0].SectionTitle);
            Assert.Equal("Content 1", result.Sections[0].SectionContent);
            Assert.Equal(2, result.Sections[1].OrderIndex);
            Assert.Equal("Section 2", result.Sections[1].SectionTitle);
            Assert.Equal("Content 2", result.Sections[1].SectionContent);
            Assert.Equal(3, result.Sections[2].OrderIndex);
            Assert.Equal("Section 3", result.Sections[2].SectionTitle);
            Assert.Equal("Content 3", result.Sections[2].SectionContent);
        }
    }
}

