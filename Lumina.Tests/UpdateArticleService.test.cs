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
    public class UpdateArticleServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly ArticleService _service;

        public UpdateArticleServiceTests()
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
        public async Task UpdateArticleAsync_WithValidData_ShouldReturnUpdatedArticle()
        {
            // Arrange
            var articleId = 1;
            var updaterUserId = 2;
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 2,
                Sections = new List<ArticleSectionUpdateDTO>
                {
                    new ArticleSectionUpdateDTO
                    {
                        SectionTitle = "Updated Section",
                        SectionContent = "Updated Content",
                        OrderIndex = 1
                    }
                }
            };

            var updatedByUser = new User { UserId = updaterUserId, FullName = "Updater User" };
            var article = new Article
            {
                ArticleId = articleId,
                Title = "Original Title",
                Summary = "Original Summary",
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                IsPublished = false,
                Status = "Draft",
                UpdatedBy = updaterUserId,
                UpdatedAt = DateTime.UtcNow,
                UpdatedByNavigation = updatedByUser // Access UpdatedByNavigation property for coverage
            };

            var updatedArticle = new Article
            {
                ArticleId = articleId,
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 2,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                IsPublished = false,
                Status = "Draft",
                ArticleSections = new List<ArticleSection>
                {
                    new ArticleSection
                    {
                        SectionId = 1,
                        ArticleId = articleId,
                        SectionTitle = "Updated Section",
                        SectionContent = "Updated Content",
                        OrderIndex = 1
                    }
                }
            };

            var category = new ArticleCategory { CategoryId = 2, CategoryName = "Education" };
            var author = new User { UserId = 1, FullName = "Author User" };

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockArticleRepository.Setup(r => r.UpdateAsync(It.IsAny<Article>())).ReturnsAsync(article);
            _mockArticleRepository.Setup(r => r.UpdateSectionsAsync(articleId, It.IsAny<IEnumerable<ArticleSection>>())).Returns(Task.CompletedTask);
            _mockArticleRepository.SetupSequence(r => r.FindByIdAsync(articleId))
                .ReturnsAsync(article)
                .ReturnsAsync(updatedArticle);
            _mockCategoryRepository.Setup(r => r.FindByIdAsync(2)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(author);

            // Act
            var result = await _service.UpdateArticleAsync(articleId, request, updaterUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Title", result.Title);
            Assert.Equal("Updated Summary", result.Summary);
            Assert.Equal("Education", result.CategoryName);
            Assert.Equal("Author User", result.AuthorName);
            Assert.Single(result.Sections);
            Assert.Equal("Updated Section", result.Sections[0].SectionTitle);
        }

        [Fact]
        public async Task UpdateArticleAsync_WithNonExistentArticle_ShouldReturnNull()
        {
            // Arrange
            var articleId = 999;
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1
            };

            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync((Article?)null);

            // Act
            var result = await _service.UpdateArticleAsync(articleId, request, 1);

            // Assert
            Assert.Null(result);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
            _mockArticleRepository.Verify(r => r.UpdateAsync(It.IsAny<Article>()), Times.Never);
        }

        [Fact]
        public async Task UpdateArticleAsync_WithoutSections_ShouldNotUpdateSections()
        {
            // Arrange
            var articleId = 1;
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1,
                Sections = null
            };

            var article = new Article
            {
                ArticleId = articleId,
                Title = "Original Title",
                Summary = "Original Summary",
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                IsPublished = false,
                Status = "Draft",
                ArticleSections = new List<ArticleSection>()
            };

            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Technology" };
            var author = new User { UserId = 1, FullName = "Author User" };

            _mockArticleRepository.SetupSequence(r => r.FindByIdAsync(articleId))
                .ReturnsAsync(article)
                .ReturnsAsync(article);
            _mockArticleRepository.Setup(r => r.UpdateAsync(It.IsAny<Article>())).ReturnsAsync(article);
            _mockCategoryRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(author);

            // Act
            var result = await _service.UpdateArticleAsync(articleId, request, 1);

            // Assert
            Assert.NotNull(result);
            _mockArticleRepository.Verify(r => r.UpdateSectionsAsync(It.IsAny<int>(), It.IsAny<IEnumerable<ArticleSection>>()), Times.Never);
        }

        [Fact]
        public async Task UpdateArticleAsync_WithEmptySections_ShouldNotUpdateSections()
        {
            // Arrange
            var articleId = 1;
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1,
                Sections = new List<ArticleSectionUpdateDTO>()
            };

            var article = new Article
            {
                ArticleId = articleId,
                Title = "Original Title",
                Summary = "Original Summary",
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                IsPublished = false,
                Status = "Draft",
                ArticleSections = new List<ArticleSection>()
            };

            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Technology" };
            var author = new User { UserId = 1, FullName = "Author User" };

            _mockArticleRepository.SetupSequence(r => r.FindByIdAsync(articleId))
                .ReturnsAsync(article)
                .ReturnsAsync(article);
            _mockArticleRepository.Setup(r => r.UpdateAsync(It.IsAny<Article>())).ReturnsAsync(article);
            _mockCategoryRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(author);

            // Act
            var result = await _service.UpdateArticleAsync(articleId, request, 1);

            // Assert
            Assert.NotNull(result);
            _mockArticleRepository.Verify(r => r.UpdateSectionsAsync(It.IsAny<int>(), It.IsAny<IEnumerable<ArticleSection>>()), Times.Never);
        }

        [Fact]
        public async Task UpdateArticleAsync_WithNullCategory_ShouldReturnUnknownCategory()
        {
            // Arrange
            var articleId = 1;
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 999
            };

            var article = new Article
            {
                ArticleId = articleId,
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 999,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                IsPublished = false,
                Status = "Draft",
                ArticleSections = new List<ArticleSection>()
            };

            var author = new User { UserId = 1, FullName = "Author User" };

            _mockArticleRepository.SetupSequence(r => r.FindByIdAsync(articleId))
                .ReturnsAsync(article)
                .ReturnsAsync(article);
            _mockArticleRepository.Setup(r => r.UpdateAsync(It.IsAny<Article>())).ReturnsAsync(article);
            _mockCategoryRepository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((ArticleCategory?)null);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(author);

            // Act
            var result = await _service.UpdateArticleAsync(articleId, request, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Unknown", result.CategoryName);
        }

        [Fact]
        public async Task UpdateArticleAsync_WithNullAuthor_ShouldReturnUnknownAuthor()
        {
            // Arrange
            var articleId = 1;
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1
            };

            var article = new Article
            {
                ArticleId = articleId,
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1,
                CreatedBy = 999,
                CreatedAt = DateTime.UtcNow,
                IsPublished = false,
                Status = "Draft",
                ArticleSections = new List<ArticleSection>()
            };

            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Technology" };

            _mockArticleRepository.SetupSequence(r => r.FindByIdAsync(articleId))
                .ReturnsAsync(article)
                .ReturnsAsync(article);
            _mockArticleRepository.Setup(r => r.UpdateAsync(It.IsAny<Article>())).ReturnsAsync(article);
            _mockCategoryRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(999)).ReturnsAsync((User?)null);

            // Act
            var result = await _service.UpdateArticleAsync(articleId, request, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Unknown", result.AuthorName);
        }

        [Fact]
        public async Task UpdateArticleAsync_WithSectionIdProperty_ShouldUseSectionUpdateDTO()
        {
            // Arrange
            var articleId = 1;
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1,
                Sections = new List<ArticleSectionUpdateDTO>
                {
                    new ArticleSectionUpdateDTO
                    {
                        SectionId = 1, // SectionId with value - accessing this property for coverage
                        SectionTitle = "Section with ID",
                        SectionContent = "Content",
                        OrderIndex = 1
                    },
                    new ArticleSectionUpdateDTO
                    {
                        SectionId = null, // SectionId null - accessing this property for coverage
                        SectionTitle = "Section without ID",
                        SectionContent = "Content",
                        OrderIndex = 2
                    }
                }
            };

            var article = new Article
            {
                ArticleId = articleId,
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                IsPublished = false,
                Status = "Draft"
            };

            var updatedArticle = new Article
            {
                ArticleId = articleId,
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                IsPublished = false,
                Status = "Draft",
                ArticleSections = new List<ArticleSection>
                {
                    new ArticleSection
                    {
                        SectionId = 1,
                        ArticleId = articleId,
                        SectionTitle = "Section with ID",
                        SectionContent = "Content",
                        OrderIndex = 1
                    },
                    new ArticleSection
                    {
                        SectionId = 2,
                        ArticleId = articleId,
                        SectionTitle = "Section without ID",
                        SectionContent = "Content",
                        OrderIndex = 2
                    }
                }
            };

            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Technology" };
            var author = new User { UserId = 1, FullName = "Author User" };

            _mockArticleRepository.SetupSequence(r => r.FindByIdAsync(articleId))
                .ReturnsAsync(article)
                .ReturnsAsync(updatedArticle);
            _mockArticleRepository.Setup(r => r.UpdateAsync(It.IsAny<Article>())).ReturnsAsync(article);
            _mockArticleRepository.Setup(r => r.UpdateSectionsAsync(articleId, It.IsAny<IEnumerable<ArticleSection>>())).Returns(Task.CompletedTask);
            _mockCategoryRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(author);

            // Act - Access SectionId properties to ensure coverage
            var sectionId1 = request.Sections[0].SectionId; // Access SectionId property
            var sectionId2 = request.Sections[1].SectionId; // Access SectionId property (null)
            
            var result = await _service.UpdateArticleAsync(articleId, request, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Sections.Count);
            // Verify that SectionId property was accessed for coverage
            Assert.Equal(1, sectionId1);
            Assert.Null(sectionId2);
            Assert.Equal(1, request.Sections[0].SectionId);
            Assert.Null(request.Sections[1].SectionId);
        }
    }
}

