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
    public class GetAllArticlesAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly ArticleService _service;

        public GetAllArticlesAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArticleService>>();
            _mockArticleRepository = new Mock<IArticleRepository>();

            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);

            _service = new ArticleService(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllArticlesAsync_WhenArticlesExist_ShouldReturnAllArticles()
        {
            // Arrange
            var articles = new List<Article>
            {
                new Article
                {
                    ArticleId = 1,
                    Title = "Article 1",
                    Summary = "Summary 1",
                    Status = "Published",
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 1,
                    CategoryId = 1,
                    CreatedByNavigation = null,
                    Category = null,
                    ArticleSections = null
                },
                new Article
                {
                    ArticleId = 2,
                    Title = "Article 2",
                    Summary = "Summary 2",
                    Status = "Draft",
                    IsPublished = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 1,
                    CategoryId = 1,
                    CreatedByNavigation = new User { FullName = "Author" },
                    Category = new ArticleCategory { CategoryName = "Category" },
                    ArticleSections = new List<ArticleSection>()
                }
            };

            _mockArticleRepository
                .Setup(repo => repo.GetAllWithCategoryAndUserAsync())
                .ReturnsAsync(articles);

            // Act
            var result = await _service.GetAllArticlesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Unknown", result[0].AuthorName);
            Assert.Equal("Author", result[1].AuthorName);
        }

        [Fact]
        public async Task GetAllArticlesAsync_WhenNoArticles_ShouldReturnEmptyList()
        {
            // Arrange
            _mockArticleRepository
                .Setup(repo => repo.GetAllWithCategoryAndUserAsync())
                .ReturnsAsync(new List<Article>());

            // Act
            var result = await _service.GetAllArticlesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllArticlesAsync_WhenArticleHasSections_ShouldReturnOrderedSections()
        {
            // Arrange
            var articles = new List<Article>
            {
                new Article
                {
                    ArticleId = 1,
                    Title = "Article 1",
                    Summary = "Summary 1",
                    Status = "Published",
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 1,
                    CategoryId = 1,
                    CreatedByNavigation = new User { FullName = "Author" },
                    Category = new ArticleCategory { CategoryName = "Category" },
                    ArticleSections = new List<ArticleSection>
                    {
                        new ArticleSection
                        {
                            SectionId = 1,
                            SectionTitle = "Section 2",
                            SectionContent = "Content 2",
                            OrderIndex = 2
                        },
                        new ArticleSection
                        {
                            SectionId = 2,
                            SectionTitle = "Section 1",
                            SectionContent = "Content 1",
                            OrderIndex = 1
                        }
                    }
                }
            };

            _mockArticleRepository
                .Setup(repo => repo.GetAllWithCategoryAndUserAsync())
                .ReturnsAsync(articles);

            // Act
            var result = await _service.GetAllArticlesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(2, result[0].Sections.Count);
            // Verify sections are ordered by OrderIndex
            Assert.Equal(1, result[0].Sections[0].OrderIndex);
            Assert.Equal("Section 1", result[0].Sections[0].SectionTitle);
            Assert.Equal("Content 1", result[0].Sections[0].SectionContent);
            Assert.Equal(2, result[0].Sections[1].OrderIndex);
            Assert.Equal("Section 2", result[0].Sections[1].SectionTitle);
            Assert.Equal("Content 2", result[0].Sections[1].SectionContent);
        }
    }
}

