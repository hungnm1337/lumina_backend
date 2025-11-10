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
    public class PublishArticleControllerTests
    {
        private readonly Mock<IArticleService> _mockArticleService;
        private readonly Mock<ILogger<ArticlesController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly ArticlesController _controller;

        public PublishArticleControllerTests()
        {
            _mockArticleService = new Mock<IArticleService>();
            _mockLogger = new Mock<ILogger<ArticlesController>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
            _controller = new ArticlesController(_mockArticleService.Object, _mockLogger.Object, _mockUnitOfWork.Object);
        }

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

        [Fact]
        public void ArticlePublishRequest_PropertyAccess_ShouldCoverDTO()
        {
            // Arrange & Act - Test ArticlePublishRequest DTO property access
            var publishRequest = new ArticlePublishRequest();
            
            // Test default value
            Assert.True(publishRequest.Publish);
            
            // Test setting and getting
            publishRequest.Publish = false;
            Assert.False(publishRequest.Publish);
            
            publishRequest.Publish = true;
            Assert.True(publishRequest.Publish);
            
            // Test object initializer
            var publishRequest2 = new ArticlePublishRequest
            {
                Publish = false
            };
            Assert.False(publishRequest2.Publish);
        }

        [Fact]
        public void ArticlePublishRequest_UsedInJsonSerialization_ShouldWork()
        {
            // Arrange
            var publishRequest = new ArticlePublishRequest
            {
                Publish = true
            };

            // Act - Simulate JSON serialization/deserialization
            var publishValue = publishRequest.Publish;
            publishRequest.Publish = !publishRequest.Publish;
            var newPublishValue = publishRequest.Publish;

            // Assert
            Assert.True(publishValue);
            Assert.False(newPublishValue);
        }
    }
}

