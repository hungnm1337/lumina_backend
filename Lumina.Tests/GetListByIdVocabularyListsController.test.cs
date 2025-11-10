using DataLayer.DTOs.Auth;
using DataLayer.Models;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.User;
using ServiceLayer.Vocabulary;
using System.Security.Claims;
using Xunit;

namespace Lumina.Tests
{
    public class GetListByIdVocabularyListsControllerTests
    {
        private readonly Mock<IVocabularyListService> _mockVocabularyListService;
        private readonly Mock<ILogger<VocabularyListsController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly VocabularyListsController _controller;

        public GetListByIdVocabularyListsControllerTests()
        {
            _mockVocabularyListService = new Mock<IVocabularyListService>();
            _mockLogger = new Mock<ILogger<VocabularyListsController>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
            _controller = new VocabularyListsController(_mockVocabularyListService.Object, _mockLogger.Object, _mockUnitOfWork.Object);
        }

        private void SetupUserClaims(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task GetListById_AsStaff_WithOwnList_ShouldReturnOk()
        {
            // Arrange
            var userId = 1;
            var listId = 1;
            var user = new User { UserId = userId, RoleId = 3 }; // Staff
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                MakeBy = userId,
                Name = "My List"
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockUnitOfWork.Setup(u => u.VocabularyLists.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);

            // Act
            var result = await _controller.GetListById(listId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetListById_AsStaff_WithOtherUserList_ShouldReturnForbid()
        {
            // Arrange
            var userId = 1;
            var listId = 1;
            var user = new User { UserId = userId, RoleId = 3 }; // Staff
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                MakeBy = 999, // Different user
                Name = "Other List"
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockUnitOfWork.Setup(u => u.VocabularyLists.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);

            // Act
            var result = await _controller.GetListById(listId);

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetListById_AsManager_WithAnyList_ShouldReturnOk()
        {
            // Arrange
            var userId = 2;
            var listId = 1;
            var user = new User { UserId = userId, RoleId = 2 }; // Manager
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                MakeBy = 999, // Different user, but Manager can view
                Name = "Any List"
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockUnitOfWork.Setup(u => u.VocabularyLists.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);

            // Act
            var result = await _controller.GetListById(listId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetListById_WithNullUserIdClaim_ShouldReturnUnauthorized()
        {
            // Arrange
            var listId = 1;
            var claims = new List<Claim>();
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.GetListById(listId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Contains("Invalid token", errorResponse.Error);
        }

        [Fact]
        public async Task GetListById_WithUserNotFound_ShouldReturnUnauthorized()
        {
            // Arrange
            var userId = 999;
            var listId = 1;
            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

            // Act
            var result = await _controller.GetListById(listId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Contains("User not found", errorResponse.Error);
        }

        [Fact]
        public async Task GetListById_WithVocabularyListNotFound_ShouldReturnNotFound()
        {
            // Arrange
            var userId = 1;
            var listId = 999;
            var user = new User { UserId = userId, RoleId = 2 };
            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockUnitOfWork.Setup(u => u.VocabularyLists.FindByIdAsync(listId)).ReturnsAsync((VocabularyList?)null);

            // Act
            var result = await _controller.GetListById(listId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Contains("not found", errorResponse.Error);
        }

        [Fact]
        public async Task GetListById_WithException_ShouldReturnInternalServerError()
        {
            // Arrange
            var userId = 1;
            var listId = 1;
            var user = new User { UserId = userId, RoleId = 2 };
            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockUnitOfWork.Setup(u => u.VocabularyLists.FindByIdAsync(listId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetListById(listId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
    }
}

