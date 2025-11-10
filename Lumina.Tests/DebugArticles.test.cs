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
using System.Linq;

namespace Lumina.Tests
{
    public class DebugArticlesTests
    {
        private readonly Mock<IArticleService> _mockArticleService;
        private readonly Mock<ILogger<ArticlesController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IArticleRepository> _mockArticleRepository;
        private readonly ArticlesController _controller;

        public DebugArticlesTests()
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
        public async Task DebugArticles_WithValidUser_ShouldReturn200Ok()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                UserId = userId,
                RoleId = 3,
                Email = "staff@example.com",
                FullName = "Staff User"
            };

            var allArticles = new List<Article>
            {
                new Article
                {
                    ArticleId = 1,
                    Title = "Article 1",
                    CreatedBy = userId,
                    Status = "Published",
                    IsPublished = true
                },
                new Article
                {
                    ArticleId = 2,
                    Title = "Article 2",
                    CreatedBy = 2,
                    Status = "Draft",
                    IsPublished = false
                }
            };

            var queryResult = new PagedResponse<ArticleResponseDTO>
            {
                Items = new List<ArticleResponseDTO>
                {
                    new ArticleResponseDTO
                    {
                        ArticleId = 1,
                        Title = "Article 1",
                        AuthorName = "Staff User",
                        Status = "Published",
                        IsPublished = true
                    }
                },
                Total = 1,
                Page = 1,
                PageSize = 1000
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockArticleRepository.Setup(r => r.GetAllWithCategoryAndUserAsync()).ReturnsAsync(allArticles);
            _mockArticleService.Setup(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
                q.CreatedBy == userId &&
                q.Page == 1 &&
                q.PageSize == 1000
            ))).ReturnsAsync(queryResult);

            // Act
            var result = await _controller.DebugArticles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.NotNull(okResult.Value);
            
            // Verify properties using reflection
            var resultType = okResult.Value.GetType();
            var properties = resultType.GetProperties();
            Assert.True(properties.Any(p => p.Name == "CurrentUserId"));
            Assert.True(properties.Any(p => p.Name == "UserRoleId"));
            Assert.True(properties.Any(p => p.Name == "TotalArticlesInDB"));
            Assert.True(properties.Any(p => p.Name == "UserArticlesCount"));
            
            // Force enumeration of UserArticles and QueryResult to cover Select statements (lines 136-143 and 145-152)
            var userArticlesProperty = properties.FirstOrDefault(p => p.Name == "UserArticles");
            if (userArticlesProperty != null)
            {
                var userArticlesValue = userArticlesProperty.GetValue(okResult.Value);
                if (userArticlesValue is System.Collections.IEnumerable userArticlesEnumerable)
                {
                    foreach (var item in userArticlesEnumerable)
                    {
                        // Enumerate to trigger Select execution
                    }
                }
            }
            
            var queryResultProperty = properties.FirstOrDefault(p => p.Name == "QueryResult");
            if (queryResultProperty != null)
            {
                var queryResultValue = queryResultProperty.GetValue(okResult.Value);
                if (queryResultValue is System.Collections.IEnumerable queryResultEnumerable)
                {
                    foreach (var item in queryResultEnumerable)
                    {
                        // Enumerate to trigger Select execution
                    }
                }
            }
            
            _mockArticleRepository.Verify(r => r.GetAllWithCategoryAndUserAsync(), Times.Once);
            _mockArticleService.Verify(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
                q.CreatedBy == userId
            )), Times.Once);
        }

        [Fact]
        public async Task DebugArticles_WithMultipleUserArticles_ShouldEnumerateSelectStatements()
        {
            // Arrange - This test ensures the Select statements in lines 136-143 and 145-152 are executed
            var userId = 1;
            var user = new User
            {
                UserId = userId,
                RoleId = 3,
                Email = "staff@example.com",
                FullName = "Staff User"
            };

            var allArticles = new List<Article>
            {
                new Article
                {
                    ArticleId = 1,
                    Title = "Article 1",
                    CreatedBy = userId,
                    Status = "Published",
                    IsPublished = true
                },
                new Article
                {
                    ArticleId = 2,
                    Title = "Article 2",
                    CreatedBy = userId,
                    Status = "Draft",
                    IsPublished = false
                },
                new Article
                {
                    ArticleId = 3,
                    Title = "Article 3",
                    CreatedBy = 2,
                    Status = "Published",
                    IsPublished = true
                }
            };

            var queryResult = new PagedResponse<ArticleResponseDTO>
            {
                Items = new List<ArticleResponseDTO>
                {
                    new ArticleResponseDTO
                    {
                        ArticleId = 1,
                        Title = "Article 1",
                        AuthorName = "Staff User",
                        Status = "Published",
                        IsPublished = true
                    },
                    new ArticleResponseDTO
                    {
                        ArticleId = 2,
                        Title = "Article 2",
                        AuthorName = "Staff User",
                        Status = "Draft",
                        IsPublished = false
                    }
                },
                Total = 2,
                Page = 1,
                PageSize = 1000
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockArticleRepository.Setup(r => r.GetAllWithCategoryAndUserAsync()).ReturnsAsync(allArticles);
            _mockArticleService.Setup(s => s.QueryAsync(It.Is<ArticleQueryParams>(q =>
                q.CreatedBy == userId &&
                q.Page == 1 &&
                q.PageSize == 1000
            ))).ReturnsAsync(queryResult);

            // Act
            var result = await _controller.DebugArticles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.NotNull(okResult.Value);
            
            // Access UserArticles property to ensure Select statement (lines 136-143) is executed
            var resultType = okResult.Value.GetType();
            var userArticlesProperty = resultType.GetProperty("UserArticles");
            Assert.NotNull(userArticlesProperty);
            var userArticlesValue = userArticlesProperty.GetValue(okResult.Value);
            Assert.NotNull(userArticlesValue);
            
            // Enumerate UserArticles to ensure Select is executed
            if (userArticlesValue is System.Collections.IEnumerable userArticlesEnumerable)
            {
                var userArticlesList = userArticlesEnumerable.Cast<object>().ToList();
                Assert.Equal(2, userArticlesList.Count); // Should have 2 articles created by userId
                
                // Access properties of each item to ensure Select projection is executed
                foreach (var item in userArticlesList)
                {
                    var itemType = item.GetType();
                    var articleIdProperty = itemType.GetProperty("ArticleId");
                    var titleProperty = itemType.GetProperty("Title");
                    var createdByProperty = itemType.GetProperty("CreatedBy");
                    var statusProperty = itemType.GetProperty("Status");
                    var isPublishedProperty = itemType.GetProperty("IsPublished");
                    
                    Assert.NotNull(articleIdProperty);
                    Assert.NotNull(titleProperty);
                    Assert.NotNull(createdByProperty);
                    Assert.NotNull(statusProperty);
                    Assert.NotNull(isPublishedProperty);
                    
                    // Access properties to ensure they are evaluated
                    var articleId = articleIdProperty.GetValue(item);
                    var title = titleProperty.GetValue(item);
                    var createdBy = createdByProperty.GetValue(item);
                    var status = statusProperty.GetValue(item);
                    var isPublished = isPublishedProperty.GetValue(item);
                    
                    Assert.NotNull(articleId);
                    Assert.NotNull(title);
                }
            }
            
            // Access QueryResult property to ensure Select statement (lines 145-152) is executed
            var queryResultProperty = resultType.GetProperty("QueryResult");
            Assert.NotNull(queryResultProperty);
            var queryResultValue = queryResultProperty.GetValue(okResult.Value);
            Assert.NotNull(queryResultValue);
            
            // Enumerate QueryResult to ensure Select is executed
            if (queryResultValue is System.Collections.IEnumerable queryResultEnumerable)
            {
                var queryResultList = queryResultEnumerable.Cast<object>().ToList();
                Assert.Equal(2, queryResultList.Count);
                
                // Access properties of each item to ensure Select projection is executed
                foreach (var item in queryResultList)
                {
                    var itemType = item.GetType();
                    var articleIdProperty = itemType.GetProperty("ArticleId");
                    var titleProperty = itemType.GetProperty("Title");
                    var authorNameProperty = itemType.GetProperty("AuthorName");
                    var statusProperty = itemType.GetProperty("Status");
                    var isPublishedProperty = itemType.GetProperty("IsPublished");
                    
                    Assert.NotNull(articleIdProperty);
                    Assert.NotNull(titleProperty);
                    Assert.NotNull(authorNameProperty);
                    Assert.NotNull(statusProperty);
                    Assert.NotNull(isPublishedProperty);
                    
                    // Access properties to ensure they are evaluated
                    var articleId = articleIdProperty.GetValue(item);
                    var title = titleProperty.GetValue(item);
                    var authorName = authorNameProperty.GetValue(item);
                    var status = statusProperty.GetValue(item);
                    var isPublished = isPublishedProperty.GetValue(item);
                    
                    Assert.NotNull(articleId);
                    Assert.NotNull(title);
                }
            }
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task DebugArticles_WithNullUserIdClaim_ShouldReturn401Unauthorized()
        {
            // Arrange
            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.DebugArticles();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid token - User ID could not be determined.", errorResponse.Error);
        }

        [Fact]
        public async Task DebugArticles_WithUserNotFound_ShouldReturn401Unauthorized()
        {
            // Arrange
            var userId = 999;
            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

            // Act
            var result = await _controller.DebugArticles();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("User not found.", errorResponse.Error);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task DebugArticles_WhenRepositoryThrowsException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                UserId = userId,
                RoleId = 3,
                Email = "staff@example.com",
                FullName = "Staff User"
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockArticleRepository.Setup(r => r.GetAllWithCategoryAndUserAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DebugArticles();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
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

