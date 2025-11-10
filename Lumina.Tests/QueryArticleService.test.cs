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
    public class QueryArticleServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly ArticleService _service;

        public QueryArticleServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockArticleRepository = new Mock<IArticleRepository>();

            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task QueryAsync_WithValidQuery_ShouldReturnPagedResponse()
        {
            // Arrange
            var query = new ArticleQueryParams
            {
                Page = 1,
                PageSize = 10,
                Search = "Test",
                CategoryId = 1,
                IsPublished = true,
                Status = "Published",
                SortBy = "createdAt",
                SortDir = "desc",
                CreatedBy = 1
            };

            var articles = new List<Article>
            {
                new Article
                {
                    ArticleId = 1,
                    Title = "Test Article",
                    Summary = "Test Summary",
                    IsPublished = true,
                    Status = "Published",
                    CreatedAt = DateTime.UtcNow,
                    CategoryId = 1,
                    CreatedBy = 1,
                    Category = new ArticleCategory { CategoryId = 1, CategoryName = "Technology" },
                    CreatedByNavigation = new User { UserId = 1, FullName = "User 1" },
                    ArticleSections = new List<ArticleSection>()
                }
            };

            _mockArticleRepository.Setup(r => r.QueryAsync(
                query.Page, query.PageSize, query.Search, query.CategoryId, query.IsPublished,
                query.Status, query.SortBy, query.SortDir, query.CreatedBy))
                .ReturnsAsync((articles, 1));

            // Act
            var result = await _service.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(1, result.Total);
            Assert.Equal(query.Page, result.Page);
            Assert.Equal(query.PageSize, result.PageSize);
            Assert.Equal("Test Article", result.Items[0].Title);
            Assert.Equal("Technology", result.Items[0].CategoryName);
            Assert.Equal("User 1", result.Items[0].AuthorName);
        }

        [Fact]
        public async Task QueryAsync_WithNullCategory_ShouldReturnUnknownCategory()
        {
            // Arrange
            var query = new ArticleQueryParams
            {
                Page = 1,
                PageSize = 10
            };

            var articles = new List<Article>
            {
                new Article
                {
                    ArticleId = 1,
                    Title = "Test Article",
                    Summary = "Test Summary",
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

            _mockArticleRepository.Setup(r => r.QueryAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<bool?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()))
                .ReturnsAsync((articles, 1));

            // Act
            var result = await _service.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("Unknown", result.Items[0].CategoryName);
        }

        [Fact]
        public async Task QueryAsync_WithNullAuthor_ShouldReturnUnknownAuthor()
        {
            // Arrange
            var query = new ArticleQueryParams
            {
                Page = 1,
                PageSize = 10
            };

            var articles = new List<Article>
            {
                new Article
                {
                    ArticleId = 1,
                    Title = "Test Article",
                    Summary = "Test Summary",
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

            _mockArticleRepository.Setup(r => r.QueryAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<bool?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()))
                .ReturnsAsync((articles, 1));

            // Act
            var result = await _service.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("Unknown", result.Items[0].AuthorName);
        }

        [Fact]
        public async Task QueryAsync_WithNullSections_ShouldReturnEmptySections()
        {
            // Arrange
            var query = new ArticleQueryParams
            {
                Page = 1,
                PageSize = 10
            };

            var articles = new List<Article>
            {
                new Article
                {
                    ArticleId = 1,
                    Title = "Test Article",
                    Summary = "Test Summary",
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

            _mockArticleRepository.Setup(r => r.QueryAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<bool?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()))
                .ReturnsAsync((articles, 1));

            // Act
            var result = await _service.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.NotNull(result.Items[0].Sections);
            Assert.Empty(result.Items[0].Sections);
        }

        [Fact]
        public async Task QueryAsync_WithMultipleSections_ShouldOrderSectionsByOrderIndex()
        {
            // Arrange
            var query = new ArticleQueryParams
            {
                Page = 1,
                PageSize = 10
            };

            var articles = new List<Article>
            {
                new Article
                {
                    ArticleId = 1,
                    Title = "Test Article",
                    Summary = "Test Summary",
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

            _mockArticleRepository.Setup(r => r.QueryAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<bool?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()))
                .ReturnsAsync((articles, 1));

            // Act
            var result = await _service.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(3, result.Items[0].Sections.Count);
            Assert.Equal("Section 1", result.Items[0].Sections[0].SectionTitle);
            Assert.Equal("Section 2", result.Items[0].Sections[1].SectionTitle);
            Assert.Equal("Section 3", result.Items[0].Sections[2].SectionTitle);
        }

        [Fact]
        public async Task QueryAsync_WithEmptyResults_ShouldReturnEmptyPagedResponse()
        {
            // Arrange
            var query = new ArticleQueryParams
            {
                Page = 1,
                PageSize = 10
            };

            var articles = new List<Article>();

            _mockArticleRepository.Setup(r => r.QueryAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<bool?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()))
                .ReturnsAsync((articles, 0));

            // Act
            var result = await _service.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.Total);
            Assert.Equal(query.Page, result.Page);
            Assert.Equal(query.PageSize, result.PageSize);
        }
    }
}

