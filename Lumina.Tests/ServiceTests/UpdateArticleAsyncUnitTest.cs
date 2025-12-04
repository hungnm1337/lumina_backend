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
    public class UpdateArticleAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArticleService>> _mockLogger;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly Mock<RepositoryLayer.User.IUserRepository> _mockUserRepository;
        private readonly ArticleService _service;

        public UpdateArticleAsyncUnitTest()
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
        public async Task UpdateArticleAsync_WhenArticleNotFound_ShouldReturnNull()
        {
            // Arrange
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1
            };

            _mockArticleRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync((Article?)null);

            // Act
            var result = await _service.UpdateArticleAsync(1, request, 1);

            // Assert
            Assert.Null(result);
            _mockArticleRepository.Verify(repo => repo.FindByIdAsync(1), Times.Once);
            _mockArticleRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Article>()), Times.Never);
        }

        [Fact]
        public async Task UpdateArticleAsync_WhenStaffUpdatesPublishedArticle_ShouldSetStatusToPending()
        {
            // Arrange
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1,
                Sections = new List<ArticleSectionUpdateDTO>()
            };

            var article = new Article
            {
                ArticleId = 1,
                Title = "Original",
                Summary = "Original Summary",
                Status = "Published",
                IsPublished = true,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                ArticleSections = new List<ArticleSection>()
            };

            var staffUser = new User { UserId = 1, RoleId = 3 };
            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Test" };
            var author = new User { UserId = 1, FullName = "Author" };

            // Reloaded article with mismatch (simulating DB issue)
            var reloadedArticleWithMismatch = new Article
            {
                ArticleId = 1,
                Title = "Updated Title",
                Status = "Published", // Mismatch
                IsPublished = true, // Mismatch
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = article.CreatedAt,
                ArticleSections = new List<ArticleSection>()
            };

            var reloadedArticleFixed = new Article
            {
                ArticleId = 1,
                Title = "Updated Title",
                Status = "Pending", // Fixed
                IsPublished = false, // Fixed
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = article.CreatedAt,
                ArticleSections = new List<ArticleSection>()
            };

            _mockArticleRepository
                .SetupSequence(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article)
                .ReturnsAsync(reloadedArticleWithMismatch)
                .ReturnsAsync(reloadedArticleFixed);

            _mockUserRepository
                .SetupSequence(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync(staffUser)
                .ReturnsAsync(author);

            _mockArticleRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Article>()))
                .ReturnsAsync((Article a) => a);

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(category);

            // Act
            var result = await _service.UpdateArticleAsync(1, request, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Pending", result.Status);
            Assert.False(result.IsPublished);
            _mockArticleRepository.Verify(repo => repo.UpdateAsync(It.Is<Article>(a => a.Status == "Pending" && a.IsPublished == false)), Times.AtLeastOnce);
        }

        [Theory]
        [InlineData(null, 3)] // Updater null
        [InlineData(2, 2)] // Non-staff (RoleId != 3)
        public async Task UpdateArticleAsync_WhenNotStaffOrNull_ShouldNotChangeStatus(int? roleId, int userId)
        {
            // Arrange
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1,
                Sections = null
            };

            var article = new Article
            {
                ArticleId = 1,
                Title = "Original",
                Status = "Published",
                IsPublished = true,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                ArticleSections = new List<ArticleSection>()
            };

            var reloadedArticle = new Article
            {
                ArticleId = 1,
                Title = "Updated Title",
                Status = "Published",
                IsPublished = true,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = article.CreatedAt,
                ArticleSections = new List<ArticleSection>()
            };

            User? updater = roleId.HasValue ? new User { UserId = userId, RoleId = roleId.Value } : null;
            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Test" };
            var author = new User { UserId = 1, FullName = "Author" };

            _mockArticleRepository
                .SetupSequence(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article)
                .ReturnsAsync(reloadedArticle);

            _mockUserRepository
                .SetupSequence(repo => repo.GetUserByIdAsync(userId))
                .ReturnsAsync(updater)
                .ReturnsAsync(author);

            _mockArticleRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Article>()))
                .ReturnsAsync((Article a) => a);

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(category);

            // Act
            var result = await _service.UpdateArticleAsync(1, request, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Published", result.Status);
            Assert.True(result.IsPublished);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Draft")]
        public async Task UpdateArticleAsync_WhenStatusIsNotPublished_ShouldNotChangeStatus(string? status)
        {
            // Arrange
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1,
                Sections = null
            };

            var article = new Article
            {
                ArticleId = 1,
                Title = "Original",
                Status = status,
                IsPublished = false,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                ArticleSections = new List<ArticleSection>()
            };

            var reloadedArticle = new Article
            {
                ArticleId = 1,
                Title = "Updated Title",
                Status = status,
                IsPublished = false,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = article.CreatedAt,
                ArticleSections = new List<ArticleSection>()
            };

            var staffUser = new User { UserId = 1, RoleId = 3 };
            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Test" };
            var author = new User { UserId = 1, FullName = "Author" };

            _mockArticleRepository
                .SetupSequence(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article)
                .ReturnsAsync(reloadedArticle);

            _mockUserRepository
                .SetupSequence(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync(staffUser)
                .ReturnsAsync(author);

            _mockArticleRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Article>()))
                .ReturnsAsync((Article a) => a);

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(category);

            // Act
            var result = await _service.UpdateArticleAsync(1, request, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(status ?? null, result.Status);
        }

        [Fact]
        public async Task UpdateArticleAsync_WhenSectionsHasItems_ShouldUpdateSections()
        {
            // Arrange
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1,
                Sections = new List<ArticleSectionUpdateDTO>
                {
                    new ArticleSectionUpdateDTO
                    {
                        SectionTitle = "Section 1",
                        SectionContent = "Content 1",
                        OrderIndex = 1
                    }
                }
            };

            var article = new Article
            {
                ArticleId = 1,
                Title = "Original",
                Status = "Draft",
                IsPublished = false,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                ArticleSections = new List<ArticleSection>()
            };

            var reloadedArticle = new Article
            {
                ArticleId = 1,
                Title = "Updated Title",
                Status = "Draft",
                IsPublished = false,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = article.CreatedAt,
                ArticleSections = new List<ArticleSection>()
            };

            var user = new User { UserId = 1, RoleId = 2 };
            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Test" };
            var author = new User { UserId = 1, FullName = "Author" };

            _mockArticleRepository
                .SetupSequence(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article)
                .ReturnsAsync(reloadedArticle);

            _mockUserRepository
                .SetupSequence(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync(user)
                .ReturnsAsync(author);

            _mockArticleRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Article>()))
                .ReturnsAsync((Article a) => a);

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(category);

            // Act
            var result = await _service.UpdateArticleAsync(1, request, 1);

            // Assert
            Assert.NotNull(result);
            _mockArticleRepository.Verify(repo => repo.UpdateSectionsAsync(1, It.IsAny<List<ArticleSection>>()), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)] // Empty list
        public async Task UpdateArticleAsync_WhenSectionsIsNullOrEmpty_ShouldNotUpdateSections(int? sectionsCount)
        {
            // Arrange
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1,
                Sections = sectionsCount.HasValue ? new List<ArticleSectionUpdateDTO>() : null
            };

            var article = new Article
            {
                ArticleId = 1,
                Title = "Original",
                Status = "Draft",
                IsPublished = false,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                ArticleSections = new List<ArticleSection>()
            };

            var reloadedArticle = new Article
            {
                ArticleId = 1,
                Title = "Updated Title",
                Status = "Draft",
                IsPublished = false,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = article.CreatedAt,
                ArticleSections = new List<ArticleSection>()
            };

            var user = new User { UserId = 1, RoleId = 2 };
            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Test" };
            var author = new User { UserId = 1, FullName = "Author" };

            _mockArticleRepository
                .SetupSequence(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article)
                .ReturnsAsync(reloadedArticle);

            _mockUserRepository
                .SetupSequence(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync(user)
                .ReturnsAsync(author);

            _mockArticleRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Article>()))
                .ReturnsAsync((Article a) => a);

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(category);

            // Act
            var result = await _service.UpdateArticleAsync(1, request, 1);

            // Assert
            Assert.NotNull(result);
            _mockArticleRepository.Verify(repo => repo.UpdateSectionsAsync(It.IsAny<int>(), It.IsAny<List<ArticleSection>>()), Times.Never);
        }

        [Fact]
        public async Task UpdateArticleAsync_WhenCategoryIsNull_ShouldReturnDTOWithUnknownCategory()
        {
            // Arrange
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1,
                Sections = null
            };

            var article = new Article
            {
                ArticleId = 1,
                Title = "Original",
                Status = "Draft",
                IsPublished = false,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                ArticleSections = new List<ArticleSection>()
            };

            var reloadedArticle = new Article
            {
                ArticleId = 1,
                Title = "Updated Title",
                Status = "Draft",
                IsPublished = false,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = article.CreatedAt,
                ArticleSections = new List<ArticleSection>()
            };

            var user = new User { UserId = 1, RoleId = 2 };
            var author = new User { UserId = 1, FullName = "Author" };

            _mockArticleRepository
                .SetupSequence(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article)
                .ReturnsAsync(reloadedArticle);

            _mockUserRepository
                .SetupSequence(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync(user)
                .ReturnsAsync(author);

            _mockArticleRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Article>()))
                .ReturnsAsync((Article a) => a);

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync((ArticleCategory?)null);

            // Act
            var result = await _service.UpdateArticleAsync(1, request, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Unknown", result.CategoryName);
        }

        [Fact]
        public async Task UpdateArticleAsync_WhenAuthorIsNull_ShouldReturnDTOWithUnknownAuthor()
        {
            // Arrange
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1,
                Sections = null
            };

            var article = new Article
            {
                ArticleId = 1,
                Title = "Original",
                Status = "Draft",
                IsPublished = false,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                ArticleSections = new List<ArticleSection>()
            };

            var reloadedArticle = new Article
            {
                ArticleId = 1,
                Title = "Updated Title",
                Status = "Draft",
                IsPublished = false,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = article.CreatedAt,
                ArticleSections = new List<ArticleSection>()
            };

            var user = new User { UserId = 1, RoleId = 2 };
            var category = new ArticleCategory { CategoryId = 1, CategoryName = "Test" };

            _mockArticleRepository
                .SetupSequence(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article)
                .ReturnsAsync(reloadedArticle);

            _mockUserRepository
                .SetupSequence(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync(user)
                .ReturnsAsync((User?)null); // Author is null

            _mockArticleRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Article>()))
                .ReturnsAsync((Article a) => a);

            _mockCategoryRepository
                .Setup(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(category);

            // Act
            var result = await _service.UpdateArticleAsync(1, request, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Unknown", result.AuthorName);
        }

        [Fact]
        public async Task UpdateArticleAsync_WhenArticleIsNullAfterReload_ShouldReturnNull()
        {
            // Arrange
            var request = new ArticleUpdateDTO
            {
                Title = "Updated Title",
                Summary = "Updated Summary",
                CategoryId = 1,
                Sections = null
            };

            var article = new Article
            {
                ArticleId = 1,
                Title = "Original",
                Status = "Draft",
                IsPublished = false,
                CategoryId = 1,
                CreatedBy = 1,
                CreatedAt = DateTime.UtcNow,
                ArticleSections = new List<ArticleSection>()
            };

            var user = new User { UserId = 1, RoleId = 2 };

            _mockArticleRepository
                .SetupSequence(repo => repo.FindByIdAsync(1))
                .ReturnsAsync(article)
                .ReturnsAsync((Article?)null); // Article is null after reload

            _mockUserRepository
                .Setup(repo => repo.GetUserByIdAsync(1))
                .ReturnsAsync(user);

            _mockArticleRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Article>()))
                .ReturnsAsync((Article a) => a);

            // Act
            var result = await _service.UpdateArticleAsync(1, request, 1);

            // Assert
            Assert.Null(result);
        }
    }
}
