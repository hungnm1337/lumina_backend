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
using ServiceLayer.Article;
using System.Security.Claims;

namespace Lumina.Tests
{
    public class RequestApprovalArticleTests
    {
        private readonly Mock<IArticleService> _mockArticleService;
        private readonly Mock<ILogger<ArticlesController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly ArticlesController _controller;

        public RequestApprovalArticleTests()
        {
            _mockArticleService = new Mock<IArticleService>();
            _mockLogger = new Mock<ILogger<ArticlesController>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockArticleRepository = new Mock<IArticleRepository>();
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
            _mockUnitOfWork.Setup(u => u.Articles).Returns(_mockArticleRepository.Object);
            _controller = new ArticlesController(_mockArticleService.Object, _mockLogger.Object, _mockUnitOfWork.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task RequestApproval_AsStaffWithOwnDraftArticle_ShouldReturn204NoContent()
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
                Title = "Draft Article",
                Summary = "Summary",
                CategoryId = 1,
                CreatedBy = userId,
                IsPublished = false,
                Status = "Draft"
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockArticleService.Setup(s => s.RequestApprovalAsync(articleId, userId)).ReturnsAsync(true);

            // Act
            var result = await _controller.RequestApproval(articleId);

            // Assert
            var noContentResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
            _mockArticleService.Verify(s => s.RequestApprovalAsync(articleId, userId), Times.Once);
        }

        [Fact]
        public async Task RequestApproval_AsStaffWithOwnRejectedArticle_ShouldReturn204NoContent()
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
                Title = "Rejected Article",
                Summary = "Summary",
                CategoryId = 1,
                CreatedBy = userId,
                IsPublished = false,
                Status = "Rejected"
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockArticleService.Setup(s => s.RequestApprovalAsync(articleId, userId)).ReturnsAsync(true);

            // Act
            var result = await _controller.RequestApproval(articleId);

            // Assert
            var noContentResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
            _mockArticleService.Verify(s => s.RequestApprovalAsync(articleId, userId), Times.Once);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task RequestApproval_WithNullUserIdClaim_ShouldReturn401Unauthorized()
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
            var result = await _controller.RequestApproval(articleId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid token.", errorResponse.Error);
        }

        [Fact]
        public async Task RequestApproval_WithUserNotFound_ShouldReturn401Unauthorized()
        {
            // Arrange
            var userId = 1;
            var articleId = 1;
            var article = new Article
            {
                ArticleId = articleId,
                Title = "Test Article",
                Summary = "Summary",
                CategoryId = 1,
                CreatedBy = userId,
                IsPublished = false,
                Status = "Draft"
            };
            SetupUserClaims(userId);
            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

            // Act
            var result = await _controller.RequestApproval(articleId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("User not found.", errorResponse.Error);
        }

        #endregion

        #region Staff Authorization Tests

        [Fact]
        public async Task RequestApproval_AsStaffWithOtherUserArticle_ShouldReturn403Forbid()
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
            var result = await _controller.RequestApproval(articleId);

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result);
            _mockArticleService.Verify(s => s.RequestApprovalAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region Manager/Admin Tests

        [Fact]
        public async Task RequestApproval_AsManager_CanRequestApprovalForAnyArticle_ShouldReturn204NoContent()
        {
            // Arrange
            var userId = 2;
            var articleId = 1;
            var managerUser = new User
            {
                UserId = userId,
                RoleId = 2, // Manager
                Email = "manager@example.com",
                FullName = "Manager User"
            };

            var article = new Article
            {
                ArticleId = articleId,
                Title = "Any User Article",
                Summary = "Summary",
                CategoryId = 1,
                CreatedBy = 999, // Different user
                IsPublished = false,
                Status = "Draft"
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(managerUser);
            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockArticleService.Setup(s => s.RequestApprovalAsync(articleId, userId)).ReturnsAsync(true);

            // Act
            var result = await _controller.RequestApproval(articleId);

            // Assert
            var noContentResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
            _mockArticleService.Verify(s => s.RequestApprovalAsync(articleId, userId), Times.Once);
        }

        #endregion

        #region Not Found Tests

        [Fact]
        public async Task RequestApproval_WithNonExistentArticle_ShouldReturn404NotFound()
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
            var result = await _controller.RequestApproval(articleId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Equal($"Article with ID {articleId} not found.", errorResponse.Error);
        }

        [Fact]
        public async Task RequestApproval_WhenServiceReturnsFalse_ShouldReturn404NotFound()
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
                Title = "Published Article",
                Summary = "Summary",
                CategoryId = 1,
                CreatedBy = userId,
                IsPublished = true,
                Status = "Published" // Cannot request approval for published article
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
            _mockArticleRepository.Setup(r => r.FindByIdAsync(articleId)).ReturnsAsync(article);
            _mockArticleService.Setup(s => s.RequestApprovalAsync(articleId, userId)).ReturnsAsync(false);

            // Act
            var result = await _controller.RequestApproval(articleId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Equal($"Article with ID {articleId} not found or cannot be submitted for approval.", errorResponse.Error);
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

