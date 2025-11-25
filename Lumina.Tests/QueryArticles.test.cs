using DataLayer.DTOs;
using DataLayer.DTOs.Article;
using DataLayer.DTOs.Auth;
using DataLayer.Models;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.User;
using ServiceLayer.Article;
using System.Security.Claims;

namespace Lumina.Tests
{
    public class QueryArticlesTests
    {
        private readonly Mock<IArticleService> _mockArticleService;
        private readonly Mock<ILogger<ArticlesController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly ArticlesController _controller;

        public QueryArticlesTests()
        {
            _mockArticleService = new Mock<IArticleService>();
            _mockLogger = new Mock<ILogger<ArticlesController>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
            _controller = new ArticlesController(_mockArticleService.Object, _mockLogger.Object, _mockUnitOfWork.Object);
        }

        #region Staff User Tests

        [Fact]
        public async Task Query_AsStaff_ShouldFilterByCreatedBy()
        {
            // Arrange
            var userId = 1;
            var staffUser = new User
            {
                UserId = userId,
                RoleId = 3,
                Email = "staff@example.com",
                FullName = "Staff User"
            };

            var query = new ArticleQueryParams
            {
                Page = 1,
                PageSize = 10,
                Search = "test"
            };

            var expectedResult = new PagedResponse<ArticleResponseDTO>
            {
                Items = new List<ArticleResponseDTO>
                {
                    new ArticleResponseDTO
                    {
                        ArticleId = 1,
                        Title = "Test Article",
                        Summary = "Summary",
                        IsPublished = true,
                        Status = "Published"
                    }
                },
                Total = 1,
                Page = 1,
                PageSize = 10
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(staffUser);
            _mockArticleService.Setup(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
                q.CreatedBy == userId &&
                q.Search == "test"
            ))).ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.Query(query);

            // Assert
            var actionResult = Assert.IsType<ActionResult<PagedResponse<ArticleResponseDTO>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var pagedResponse = Assert.IsType<PagedResponse<ArticleResponseDTO>>(okResult.Value);
            Assert.Single(pagedResponse.Items);
            Assert.Equal(userId, query.CreatedBy);
            _mockArticleService.Verify(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
                q.CreatedBy == userId
            )), Times.Once);
        }

        #endregion

        #region Manager/Admin User Tests

        [Fact]
        public async Task Query_AsManager_ShouldNotFilterByCreatedBy()
        {
            // Arrange
            var userId = 2;
            var managerUser = new User
            {
                UserId = userId,
                RoleId = 2,
                Email = "manager@example.com",
                FullName = "Manager User"
            };

            var query = new ArticleQueryParams
            {
                Page = 1,
                PageSize = 10,
                Search = "test"
            };

            var expectedResult = new PagedResponse<ArticleResponseDTO>
            {
                Items = new List<ArticleResponseDTO>
                {
                    new ArticleResponseDTO
                    {
                        ArticleId = 1,
                        Title = "Test Article",
                        Summary = "Summary",
                        IsPublished = true,
                        Status = "Published"
                    }
                },
                Total = 1,
                Page = 1,
                PageSize = 10
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(managerUser);
            _mockArticleService.Setup(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
                q.CreatedBy == null &&
                q.Search == "test"
            ))).ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.Query(query);

            // Assert
            var actionResult = Assert.IsType<ActionResult<PagedResponse<ArticleResponseDTO>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var pagedResponse = Assert.IsType<PagedResponse<ArticleResponseDTO>>(okResult.Value);
            Assert.Single(pagedResponse.Items);
            Assert.Null(query.CreatedBy);
            _mockArticleService.Verify(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
                q.CreatedBy == null
            )), Times.Once);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task Query_WithNullUserIdClaim_ShouldReturn401Unauthorized()
        {
            // Arrange
            var query = new ArticleQueryParams { Page = 1, PageSize = 10 };
            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var expectedResult = new PagedResponse<ArticleResponseDTO>
            {
                Items = new List<ArticleResponseDTO>
                {
                    new ArticleResponseDTO
                    {
                        ArticleId = 1,
                        Title = "Published Article",
                        Summary = "Summary",
                        IsPublished = true,
                        Status = "Published"
                    }
                },
                Total = 1,
                Page = 1,
                PageSize = 10
            };

            _mockArticleService.Setup(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
                q.IsPublished == true &&
                q.Status == "Published"
            ))).ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.Query(query);

            // Assert
            var actionResult = Assert.IsType<ActionResult<PagedResponse<ArticleResponseDTO>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var pagedResponse = Assert.IsType<PagedResponse<ArticleResponseDTO>>(okResult.Value);
            Assert.NotNull(pagedResponse);
            _mockArticleService.Verify(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
                q.IsPublished == true &&
                q.Status == "Published"
            )), Times.Once);
        }

        [Fact]
        public async Task Query_WithUserNotFound_ShouldReturn401Unauthorized()
        {
            // Arrange
            var userId = 999;
            var query = new ArticleQueryParams { Page = 1, PageSize = 10 };
            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

            var expectedResult = new PagedResponse<ArticleResponseDTO>
            {
                Items = new List<ArticleResponseDTO>
                {
                    new ArticleResponseDTO
                    {
                        ArticleId = 1,
                        Title = "Published Article",
                        Summary = "Summary",
                        IsPublished = true,
                        Status = "Published"
                    }
                },
                Total = 1,
                Page = 1,
                PageSize = 10
            };

            _mockArticleService.Setup(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
                q.IsPublished == true &&
                q.Status == "Published"
            ))).ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.Query(query);

            // Assert
            var actionResult = Assert.IsType<ActionResult<PagedResponse<ArticleResponseDTO>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var pagedResponse = Assert.IsType<PagedResponse<ArticleResponseDTO>>(okResult.Value);
            Assert.NotNull(pagedResponse);
            _mockArticleService.Verify(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
                q.IsPublished == true &&
                q.Status == "Published"
            )), Times.Once);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task Query_WhenServiceThrowsException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var userId = 2;
            var managerUser = new User
            {
                UserId = userId,
                RoleId = 2,
                Email = "manager@example.com",
                FullName = "Manager User"
            };

            var query = new ArticleQueryParams { Page = 1, PageSize = 10 };
            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(managerUser);
            _mockArticleService.Setup(s => s.QueryAsync(It.IsAny<ArticleQueryParams>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Query(query);

            // Assert
            var actionResult = Assert.IsType<ActionResult<PagedResponse<ArticleResponseDTO>>>(result);
            var statusCodeResult = Assert.IsType<ObjectResult>(actionResult.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("An internal server error occurred. Please try again later.", errorResponse.Error);
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

