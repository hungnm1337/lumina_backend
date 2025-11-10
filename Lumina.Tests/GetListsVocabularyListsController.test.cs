using DataLayer.DTOs.Auth;
using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.User;
using ServiceLayer.Vocabulary;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Xunit;

namespace Lumina.Tests
{
    public class GetListsVocabularyListsControllerTests
    {
        private readonly Mock<IVocabularyListService> _mockVocabularyListService;
        private readonly Mock<ILogger<VocabularyListsController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly VocabularyListsController _controller;

        public GetListsVocabularyListsControllerTests()
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
        public async Task GetLists_AsStaff_ShouldReturnUserLists()
        {
            // Arrange
            var userId = 1;
            var user = new User { UserId = userId, RoleId = 3 }; // Staff
            var lists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "Staff List" }
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockVocabularyListService
                .Setup(s => s.GetListsByUserAsync(userId, null))
                .ReturnsAsync(lists);

            // Act
            var result = await _controller.GetLists(null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultLists = Assert.IsAssignableFrom<IEnumerable<VocabularyListDTO>>(okResult.Value);
            Assert.Single(resultLists);
            _mockVocabularyListService.Verify(s => s.GetListsByUserAsync(userId, null), Times.Once);
        }

        [Fact]
        public async Task GetLists_AsManager_ShouldReturnAllLists()
        {
            // Arrange
            var userId = 2;
            var user = new User { UserId = userId, RoleId = 2 }; // Manager
            var lists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "List 1" },
                new VocabularyListDTO { VocabularyListId = 2, Name = "List 2" }
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockVocabularyListService
                .Setup(s => s.GetListsAsync(null))
                .ReturnsAsync(lists);

            // Act
            var result = await _controller.GetLists(null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultLists = Assert.IsAssignableFrom<IEnumerable<VocabularyListDTO>>(okResult.Value);
            Assert.Equal(2, resultLists.Count());
            _mockVocabularyListService.Verify(s => s.GetListsAsync(null), Times.Once);
        }

        [Fact]
        public async Task GetLists_WithSearchTerm_ShouldPassSearchTerm()
        {
            // Arrange
            var userId = 2;
            var user = new User { UserId = userId, RoleId = 2 }; // Manager
            var searchTerm = "test";
            var lists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "Test List" }
            };

            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockVocabularyListService
                .Setup(s => s.GetListsAsync(searchTerm))
                .ReturnsAsync(lists);

            // Act
            var result = await _controller.GetLists(searchTerm);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockVocabularyListService.Verify(s => s.GetListsAsync(searchTerm), Times.Once);
        }

        [Fact]
        public async Task GetLists_WithNullUserIdClaim_ShouldReturnUnauthorized()
        {
            // Arrange
            var claims = new List<Claim>();
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.GetLists(null);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Contains("Invalid token", errorResponse.Error);
        }

        [Fact]
        public async Task GetLists_WithUserNotFound_ShouldReturnUnauthorized()
        {
            // Arrange
            var userId = 999;
            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

            // Act
            var result = await _controller.GetLists(null);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Contains("User not found", errorResponse.Error);
        }

        [Fact]
        public async Task GetLists_WithException_ShouldReturnInternalServerError()
        {
            // Arrange
            var userId = 1;
            var user = new User { UserId = userId, RoleId = 2 };
            SetupUserClaims(userId);
            _mockUserRepository.Setup(r => r.GetUserByIdAsync(userId)).ReturnsAsync(user);
            _mockVocabularyListService
                .Setup(s => s.GetListsAsync(null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetLists(null);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Contains("internal server error", errorResponse.Error);
        }
    }
}

