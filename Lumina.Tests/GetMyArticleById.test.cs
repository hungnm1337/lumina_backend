using DataLayer.DTOs;
using DataLayer.DTOs.Article;
using DataLayer.DTOs.Auth;
using DataLayer.Models;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.User;
using System.Security.Claims;

namespace Lumina.Tests
{
    public class GetMyArticleByIdTests
    {
        private readonly Mock<IArticleService> _mockArticleService;
        private readonly Mock<ILogger<ArticlesController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly ArticlesController _controller;

        public GetMyArticleByIdTests()
        {
            _mockArticleService = new Mock<IArticleService>();
            _mockLogger = new Mock<ILogger<ArticlesController>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockArticleRepository = new Mock<IArticleRepository>();
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);
            _mockUnitOfWork.Setup(u => u.Categories).Returns(_mockCategoryRepository.Object);
            _controller = new ArticlesController(_mockArticleService.Object, _mockLogger.Object, _mockUnitOfWork.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task GetMyArticleById_AsStaffWithOwnArticle_ShouldReturn200Ok()
        {
            // Arrange
            var userId = 1;
            var articleId = 1;
            var staffUser = new User
            {
                UserId = userId,
                RoleId = 3,
                Email = "staff@example.com",
                FullName = "Staff User"
            };

            var article = new Article
            {
                ArticleId = articleId,
                Title = "My Article",
                Summary = "Summary",
                CategoryId = 1,
                CreatedBy = userId,
                IsPublished = false,
                Status = "Draft",
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

            var category = new ArticleCategory
            {
                CategoryId = 1,
                CategoryName = "Technology"
            };

            var author = new User
            {
                UserId = userId,
                FullName = "Staff User"
            };

            SetupUserClaims(userId);
            // Setup sequence: first call returns staffUser (for role check), subsequent calls return author (for author name)
            _mockUserRepository.SetupSequence(r => r.GetUserByIdAsync(userId))
                .ReturnsAsync(staffUser)
                .ReturnsAsync(author);
            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockCategoryRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(category);

            // Act
            var result = await _controller.GetMyArticleById(articleId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var articleResponse = Assert.IsType<ArticleResponseDTO>(okResult.Value);
            Assert.Equal(articleId, articleResponse.ArticleId);
            Assert.Equal("My Article", articleResponse.Title);
            Assert.Single(articleResponse.Sections);
            Assert.Equal("Section 1", articleResponse.Sections[0].SectionTitle);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Exactly(2));
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task GetMyArticleById_WithNullUserIdClaim_ShouldReturn401Unauthorized()
        {
            // Arrange
            var articleId = 1;
            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.GetMyArticleById(articleId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid token - User ID could not be determined.", errorResponse.Error);
        }

        [Fact]
        public async Task GetMyArticleById_WithUserNotFound_ShouldReturn401Unauthorized()
        {
            // Arrange
            var userId = 1;
            var articleId = 1;
            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

            // Act
            var result = await _controller.GetMyArticleById(articleId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("User not found.", errorResponse.Error);
        }

        #endregion

        #region Staff Authorization Tests

        [Fact]
        public async Task GetMyArticleById_AsStaffWithOtherUserArticle_ShouldReturn403Forbid()
        {
            // Arrange
            var userId = 1;
            var articleId = 1;
            var staffUser = new User
            {
                UserId = userId,
                RoleId = 3,
                Email = "staff@example.com",
                FullName = "Staff User"
            };

            var article = new Article
            {
                ArticleId = articleId,
                Title = "Other User Article",
                Summary = "Summary",
                CategoryId = 1,
                CreatedBy = 999, // Different user
                IsPublished = false,
                Status = "Draft"
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);

            // Act
            var result = await _controller.GetMyArticleById(articleId);

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
        }

        #endregion

        #region Not Found Tests

        [Fact]
        public async Task GetMyArticleById_WithNonExistentArticle_ShouldReturn404NotFound()
        {
            // Arrange
            var userId = 1;
            var articleId = 999;
            var staffUser = new User
            {
                UserId = userId,
                RoleId = 3,
                Email = "staff@example.com",
                FullName = "Staff User"
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync((Article?)null);

            // Act
            var result = await _controller.GetMyArticleById(articleId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Equal($"Article with ID {articleId} not found.", errorResponse.Error);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task GetMyArticleById_WhenRepositoryThrowsException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var userId = 1;
            var articleId = 1;
            var staffUser = new User
            {
                UserId = userId,
                RoleId = 3,
                Email = "staff@example.com",
                FullName = "Staff User"
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetMyArticleById(articleId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("An internal server error occurred. Please try again later.", errorResponse.Error);
        }

        #endregion

        #region Manager/Admin Tests

        [Fact]
        public async Task GetMyArticleById_AsManager_ShouldReturn200Ok()
        {
            // Arrange
            var userId = 2;
            var articleId = 1;
            var managerUser = new User
            {
                UserId = userId,
                RoleId = 2,
                Email = "manager@example.com",
                FullName = "Manager User"
            };

            var article = new Article
            {
                ArticleId = articleId,
                Title = "Any Article",
                Summary = "Summary",
                CategoryId = 1,
                CreatedBy = 1, // Different user
                IsPublished = false,
                Status = "Draft",
                ArticleSections = new List<ArticleSection>
                {
                    new ArticleSection
                    {
                        SectionId = 1,
                        ArticleId = articleId,
                        SectionTitle = "Section 1",
                        SectionContent = "Content 1",
                        OrderIndex = 1
                    },
                    new ArticleSection
                    {
                        SectionId = 2,
                        ArticleId = articleId,
                        SectionTitle = "Section 2",
                        SectionContent = "Content 2",
                        OrderIndex = 2
                    }
                }
            };

            var category = new ArticleCategory
            {
                CategoryId = 1,
                CategoryName = "Technology"
            };

            var author = new User
            {
                UserId = 1,
                FullName = "Author User"
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(managerUser);
            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockCategoryRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(author);

            // Act
            var result = await _controller.GetMyArticleById(articleId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var articleResponse = Assert.IsType<ArticleResponseDTO>(okResult.Value);
            Assert.Equal(articleId, articleResponse.ArticleId);
            Assert.Equal("Any Article", articleResponse.Title);
            Assert.Equal(2, articleResponse.Sections.Count);
            Assert.Equal("Section 1", articleResponse.Sections[0].SectionTitle);
            _mockArticleRepository.Verify(r => r.FindByIdAsync(articleId), Times.Once);
        }

        [Fact]
        public async Task GetMyArticleById_AsManagerWithNullCategory_ShouldReturn200OkWithUnknownCategory()
        {
            // Arrange
            var userId = 2;
            var articleId = 1;
            var managerUser = new User
            {
                UserId = userId,
                RoleId = 2,
                Email = "manager@example.com",
                FullName = "Manager User"
            };

            var article = new Article
            {
                ArticleId = articleId,
                Title = "Any Article",
                Summary = "Summary",
                CategoryId = 1,
                CreatedBy = 1,
                IsPublished = false,
                Status = "Draft",
                ArticleSections = new List<ArticleSection>()
            };

            var author = new User
            {
                UserId = 1,
                FullName = "Author User"
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(managerUser);
            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockCategoryRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync((ArticleCategory?)null);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(author);

            // Act
            var result = await _controller.GetMyArticleById(articleId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var articleResponse = Assert.IsType<ArticleResponseDTO>(okResult.Value);
            Assert.Equal("Unknown", articleResponse.CategoryName);
            Assert.Equal("Author User", articleResponse.AuthorName);
        }

        [Fact]
        public async Task GetMyArticleById_AsManagerWithNullAuthor_ShouldReturn200OkWithUnknownAuthor()
        {
            // Arrange
            var userId = 2;
            var articleId = 1;
            var managerUser = new User
            {
                UserId = userId,
                RoleId = 2,
                Email = "manager@example.com",
                FullName = "Manager User"
            };

            var article = new Article
            {
                ArticleId = articleId,
                Title = "Any Article",
                Summary = "Summary",
                CategoryId = 1,
                CreatedBy = 999, // Non-existent user
                IsPublished = false,
                Status = "Draft",
                ArticleSections = new List<ArticleSection>()
            };

            var category = new ArticleCategory
            {
                CategoryId = 1,
                CategoryName = "Technology"
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(managerUser);
            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockCategoryRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(category);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(999)).ReturnsAsync((User?)null);

            // Act
            var result = await _controller.GetMyArticleById(articleId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var articleResponse = Assert.IsType<ArticleResponseDTO>(okResult.Value);
            Assert.Equal("Unknown", articleResponse.AuthorName);
            Assert.Equal("Technology", articleResponse.CategoryName);
        }

        [Fact]
        public async Task GetMyArticleById_AsManagerWithArticleNotFound_ShouldReturn404NotFound()
        {
            // Arrange
            var userId = 2;
            var articleId = 999;
            var managerUser = new User
            {
                UserId = userId,
                RoleId = 2,
                Email = "manager@example.com",
                FullName = "Manager User"
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(managerUser);
            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync((Article?)null);

            // Act
            var result = await _controller.GetMyArticleById(articleId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Equal($"Article with ID {articleId} not found.", errorResponse.Error);
        }

        #endregion

        #region Helper Methods

        private void SetupUserClaims(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        #endregion
    }
}

