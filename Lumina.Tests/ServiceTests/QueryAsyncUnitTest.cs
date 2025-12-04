using Xunit;
using Moq;
using ServiceLayer.Article;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer;
using DataLayer.DTOs.Article;
using DataLayer.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class QueryAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly ArticleService _service;

        public QueryAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockArticleRepository = new Mock<IArticleRepository>();

            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task QueryAsync_WhenArticlesExist_ShouldReturnPagedResponse()
        {
            // Arrange
            var query = new ArticleQueryParams
            {
                Page = 1,
                PageSize = 10,
                SortBy = "createdAt",
                SortDir = "desc"
            };

            var articles = new List<Article>
            {
                new Article
                {
                    ArticleId = 1,
                    Title = "Article 1",
                    Status = "Published",
                    IsPublished = true,
                    CreatedBy = 1,
                    CategoryId = 1,
                    CreatedByNavigation = new User { FullName = "Author" },
                    Category = new ArticleCategory { CategoryName = "Category" },
                    ArticleSections = new List<ArticleSection>()
                }
            };

            _mockArticleRepository
                .Setup(repo => repo.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", null))
                .ReturnsAsync((articles, 1));

            // Act
            var result = await _service.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Items.Count);
            Assert.Equal(1, result.Total);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
        }

        [Fact]
        public async Task QueryAsync_WhenNoArticles_ShouldReturnEmptyPagedResponse()
        {
            // Arrange
            var query = new ArticleQueryParams
            {
                Page = 1,
                PageSize = 10
            };

            _mockArticleRepository
                .Setup(repo => repo.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", null))
                .ReturnsAsync((new List<Article>(), 0));

            // Act
            var result = await _service.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.Total);
        }

        [Fact]
        public async Task QueryAsync_WhenCategoryIsNull_ShouldReturnDTOWithUnknownCategory()
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
                    Title = "Article 1",
                    Status = "Published",
                    IsPublished = true,
                    CreatedBy = 1,
                    CategoryId = 1,
                    CreatedByNavigation = new User { FullName = "Author" },
                    Category = null, // Category is null
                    ArticleSections = new List<ArticleSection>()
                }
            };

            _mockArticleRepository
                .Setup(repo => repo.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", null))
                .ReturnsAsync((articles, 1));

            // Act
            var result = await _service.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("Unknown", result.Items[0].CategoryName);
        }

        [Fact]
        public async Task QueryAsync_WhenAuthorIsNull_ShouldReturnDTOWithUnknownAuthor()
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
                    Title = "Article 1",
                    Status = "Published",
                    IsPublished = true,
                    CreatedBy = 1,
                    CategoryId = 1,
                    CreatedByNavigation = null, // Author is null
                    Category = new ArticleCategory { CategoryName = "Category" },
                    ArticleSections = new List<ArticleSection>()
                }
            };

            _mockArticleRepository
                .Setup(repo => repo.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", null))
                .ReturnsAsync((articles, 1));

            // Act
            var result = await _service.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("Unknown", result.Items[0].AuthorName);
        }

        [Fact]
        public async Task QueryAsync_WhenArticleSectionsIsNull_ShouldReturnEmptySections()
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
                    Title = "Article 1",
                    Status = "Published",
                    IsPublished = true,
                    CreatedBy = 1,
                    CategoryId = 1,
                    CreatedByNavigation = new User { FullName = "Author" },
                    Category = new ArticleCategory { CategoryName = "Category" },
                    ArticleSections = null // ArticleSections is null
                }
            };

            _mockArticleRepository
                .Setup(repo => repo.QueryAsync(1, 10, null, null, null, null, "createdAt", "desc", null))
                .ReturnsAsync((articles, 1));

            // Act
            var result = await _service.QueryAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Empty(result.Items[0].Sections);
        }
    }
}

