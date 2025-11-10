using DataLayer.DTOs.Article;
using DataLayer.Models;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Article;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class GetAllArticlesServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly ArticleService _service;

        public GetAllArticlesServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockArticleRepository = new Mock<IArticleRepository>();

            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllArticlesAsync_ShouldReturnAllArticles()
        {
            // Arrange
            var articles = new List<Article>
            {
                new Article
                {
                    ArticleId = 1,
                    Title = "Article 1",
                    Summary = "Summary 1",
                    IsPublished = true,
                    Status = "Published",
                    CreatedAt = DateTime.UtcNow,
                    CategoryId = 1,
                    CreatedBy = 1,
                    Category = new ArticleCategory { CategoryId = 1, CategoryName = "Technology" },
                    CreatedByNavigation = new User { UserId = 1, FullName = "User 1" },
                    ArticleSections = new List<ArticleSection>
                    {
                        new ArticleSection { SectionId = 1, SectionTitle = "Section 1", SectionContent = "Content 1", OrderIndex = 1 }
                    }
                },
                new Article
                {
                    ArticleId = 2,
                    Title = "Article 2",
                    Summary = "Summary 2",
                    IsPublished = false,
                    Status = "Draft",
                    CreatedAt = DateTime.UtcNow,
                    CategoryId = 2,
                    CreatedBy = 2,
                    Category = new ArticleCategory { CategoryId = 2, CategoryName = "Education" },
                    CreatedByNavigation = new User { UserId = 2, FullName = "User 2" },
                    ArticleSections = new List<ArticleSection>()
                }
            };

            _mockArticleRepository.Setup(r => r.GetAllWithCategoryAndUserAsync()).ReturnsAsync(articles);

            // Act
            var result = await _service.GetAllArticlesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Article 1", result[0].Title);
            Assert.Equal("Article 2", result[1].Title);
            Assert.Equal("Technology", result[0].CategoryName);
            Assert.Equal("User 1", result[0].AuthorName);
            Assert.Single(result[0].Sections);
            Assert.Empty(result[1].Sections);
        }

        [Fact]
        public async Task GetAllArticlesAsync_WithNullCategory_ShouldReturnUnknownCategory()
        {
            // Arrange
            var articles = new List<Article>
            {
                new Article
                {
                    ArticleId = 1,
                    Title = "Article 1",
                    Summary = "Summary 1",
                    IsPublished = true,
                    Status = "Published",
                    CreatedAt = DateTime.UtcNow,
                    CategoryId = 1,
                    CreatedBy = 1,
                    Category = null,
                    CreatedByNavigation = new User { UserId = 1, FullName = "User 1" },
                    ArticleSections = new List<ArticleSection>()
                }
            };

            _mockArticleRepository.Setup(r => r.GetAllWithCategoryAndUserAsync()).ReturnsAsync(articles);

            // Act
            var result = await _service.GetAllArticlesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Unknown", result[0].CategoryName);
        }

        [Fact]
        public async Task GetAllArticlesAsync_WithNullAuthor_ShouldReturnUnknownAuthor()
        {
            // Arrange
            var articles = new List<Article>
            {
                new Article
                {
                    ArticleId = 1,
                    Title = "Article 1",
                    Summary = "Summary 1",
                    IsPublished = true,
                    Status = "Published",
                    CreatedAt = DateTime.UtcNow,
                    CategoryId = 1,
                    CreatedBy = 1,
                    Category = new ArticleCategory { CategoryId = 1, CategoryName = "Technology" },
                    CreatedByNavigation = null,
                    ArticleSections = new List<ArticleSection>()
                }
            };

            _mockArticleRepository.Setup(r => r.GetAllWithCategoryAndUserAsync()).ReturnsAsync(articles);

            // Act
            var result = await _service.GetAllArticlesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Unknown", result[0].AuthorName);
        }

        [Fact]
        public async Task GetAllArticlesAsync_WithNullSections_ShouldReturnEmptySections()
        {
            // Arrange
            var articles = new List<Article>
            {
                new Article
                {
                    ArticleId = 1,
                    Title = "Article 1",
                    Summary = "Summary 1",
                    IsPublished = true,
                    Status = "Published",
                    CreatedAt = DateTime.UtcNow,
                    CategoryId = 1,
                    CreatedBy = 1,
                    Category = new ArticleCategory { CategoryId = 1, CategoryName = "Technology" },
                    CreatedByNavigation = new User { UserId = 1, FullName = "User 1" },
                    ArticleSections = null
                }
            };

            _mockArticleRepository.Setup(r => r.GetAllWithCategoryAndUserAsync()).ReturnsAsync(articles);

            // Act
            var result = await _service.GetAllArticlesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.NotNull(result[0].Sections);
            Assert.Empty(result[0].Sections);
        }

        [Fact]
        public async Task GetAllArticlesAsync_WithMultipleSections_ShouldOrderSectionsByOrderIndex()
        {
            // Arrange
            var articles = new List<Article>
            {
                new Article
                {
                    ArticleId = 1,
                    Title = "Article 1",
                    Summary = "Summary 1",
                    IsPublished = true,
                    Status = "Published",
                    CreatedAt = DateTime.UtcNow,
                    CategoryId = 1,
                    CreatedBy = 1,
                    Category = new ArticleCategory { CategoryId = 1, CategoryName = "Technology" },
                    CreatedByNavigation = new User { UserId = 1, FullName = "User 1" },
                    ArticleSections = new List<ArticleSection>
                    {
                        new ArticleSection { SectionId = 3, SectionTitle = "Section 3", SectionContent = "Content 3", OrderIndex = 3 },
                        new ArticleSection { SectionId = 1, SectionTitle = "Section 1", SectionContent = "Content 1", OrderIndex = 1 },
                        new ArticleSection { SectionId = 2, SectionTitle = "Section 2", SectionContent = "Content 2", OrderIndex = 2 }
                    }
                }
            };

            _mockArticleRepository.Setup(r => r.GetAllWithCategoryAndUserAsync()).ReturnsAsync(articles);

            // Act
            var result = await _service.GetAllArticlesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(3, result[0].Sections.Count);
            Assert.Equal("Section 1", result[0].Sections[0].SectionTitle);
            Assert.Equal("Section 2", result[0].Sections[1].SectionTitle);
            Assert.Equal("Section 3", result[0].Sections[2].SectionTitle);
        }

        [Fact]
        public async Task GetAllArticlesAsync_WithEmptyList_ShouldReturnEmptyList()
        {
            // Arrange
            var articles = new List<Article>();

            _mockArticleRepository.Setup(r => r.GetAllWithCategoryAndUserAsync()).ReturnsAsync(articles);

            // Act
            var result = await _service.GetAllArticlesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}

