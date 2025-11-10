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
    public class RequestApprovalVocabularyListsControllerTests
    {
        private readonly Mock<IVocabularyListService> _mockVocabularyListService;
        private readonly Mock<ILogger<VocabularyListsController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly VocabularyListsController _controller;

        public RequestApprovalVocabularyListsControllerTests()
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
        public async Task RequestApproval_AsStaff_WithOwnDraftList_ShouldReturnNoContent()
        {
            // Arrange
            var userId = 1;
            var listId = 1;
            var user = new User { UserId = userId, RoleId = 3 }; // Staff
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                MakeBy = userId,
                Status = "Draft"
            };

            SetupUserClaims(userId);
            _mockUnitOfWork.Setup(u => u.VocabularyLists.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockVocabularyListService
                .Setup(s => s.RequestApprovalAsync(listId, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.RequestApproval(listId);

            // Assert
            var noContentResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(204, noContentResult.StatusCode);
            _mockVocabularyListService.Verify(s => s.RequestApprovalAsync(listId, userId), Times.Once);
        }

        [Fact]
        public async Task RequestApproval_AsStaff_WithOtherUserList_ShouldReturnForbid()
        {
            // Arrange
            var userId = 1;
            var listId = 1;
            var user = new User { UserId = userId, RoleId = 3 }; // Staff
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                MakeBy = 999, // Different user
                Status = "Draft"
            };

            SetupUserClaims(userId);
            _mockUnitOfWork.Setup(u => u.VocabularyLists.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _controller.RequestApproval(listId);

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result);
            _mockVocabularyListService.Verify(s => s.RequestApprovalAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task RequestApproval_WithNullUserIdClaim_ShouldReturnUnauthorized()
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
            var result = await _controller.RequestApproval(listId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Contains("Invalid token", errorResponse.Error);
        }

        [Fact]
        public async Task RequestApproval_WithVocabularyListNotFound_ShouldReturnNotFound()
        {
            // Arrange
            var userId = 1;
            var listId = 999;
            var user = new User { UserId = userId, RoleId = 3 };
            SetupUserClaims(userId);
            _mockUnitOfWork.Setup(u => u.VocabularyLists.FindByIdAsync(listId)).ReturnsAsync((VocabularyList?)null);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _controller.RequestApproval(listId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Contains("not found", errorResponse.Error);
        }

        [Fact]
        public async Task RequestApproval_WithUserNotFound_ShouldReturnUnauthorized()
        {
            // Arrange
            var userId = 999;
            var listId = 1;
            var vocabularyList = new VocabularyList { VocabularyListId = listId };
            SetupUserClaims(userId);
            _mockUnitOfWork.Setup(u => u.VocabularyLists.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

            // Act
            var result = await _controller.RequestApproval(listId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Contains("User not found", errorResponse.Error);
        }

        [Fact]
        public async Task RequestApproval_WithServiceReturningFalse_ShouldReturnNotFound()
        {
            // Arrange
            var userId = 1;
            var listId = 1;
            var user = new User { UserId = userId, RoleId = 3 };
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                MakeBy = userId,
                Status = "Published" // Cannot request approval
            };

            SetupUserClaims(userId);
            _mockUnitOfWork.Setup(u => u.VocabularyLists.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockVocabularyListService
                .Setup(s => s.RequestApprovalAsync(listId, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.RequestApproval(listId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Contains("cannot be submitted", errorResponse.Error);
        }

        [Fact]
        public async Task RequestApproval_WithException_ShouldReturnInternalServerError()
        {
            // Arrange
            var userId = 1;
            var listId = 1;
            var user = new User { UserId = userId, RoleId = 3 };
            var vocabularyList = new VocabularyList { VocabularyListId = listId, MakeBy = userId };
            SetupUserClaims(userId);
            _mockUnitOfWork.Setup(u => u.VocabularyLists.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockVocabularyListService
                .Setup(s => s.RequestApprovalAsync(listId, userId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.RequestApproval(listId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
    }
}

