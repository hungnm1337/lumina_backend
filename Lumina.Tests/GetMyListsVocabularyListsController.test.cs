using DataLayer.DTOs.Auth;
using DataLayer.DTOs.Vocabulary;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Vocabulary;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace Lumina.Tests
{
    public class GetMyListsVocabularyListsControllerTests
    {
        private readonly Mock<IVocabularyListService> _mockVocabularyListService;
        private readonly Mock<ILogger<VocabularyListsController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly VocabularyListsController _controller;

        public GetMyListsVocabularyListsControllerTests()
        {
            _mockVocabularyListService = new Mock<IVocabularyListService>();
            _mockLogger = new Mock<ILogger<VocabularyListsController>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
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
        public async Task GetMyLists_WithValidUserId_ShouldReturnUserLists()
        {
            // Arrange
            var userId = 1;
            var lists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "My List" }
            };

            SetupUserClaims(userId);
            _mockVocabularyListService
                .Setup(s => s.GetListsByUserAsync(userId, null))
                .ReturnsAsync(lists);

            // Act
            var result = await _controller.GetMyLists(null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultLists = Assert.IsAssignableFrom<IEnumerable<VocabularyListDTO>>(okResult.Value);
            Assert.Single(resultLists);
            _mockVocabularyListService.Verify(s => s.GetListsByUserAsync(userId, null), Times.Once);
        }

        [Fact]
        public async Task GetMyLists_WithSearchTerm_ShouldPassSearchTerm()
        {
            // Arrange
            var userId = 1;
            var searchTerm = "test";
            var lists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "Test List" }
            };

            SetupUserClaims(userId);
            _mockVocabularyListService
                .Setup(s => s.GetListsByUserAsync(userId, searchTerm))
                .ReturnsAsync(lists);

            // Act
            var result = await _controller.GetMyLists(searchTerm);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockVocabularyListService.Verify(s => s.GetListsByUserAsync(userId, searchTerm), Times.Once);
        }

        [Fact]
        public async Task GetMyLists_WithNullUserIdClaim_ShouldReturnUnauthorized()
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
            var result = await _controller.GetMyLists(null);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Contains("Invalid token", errorResponse.Error);
        }

        [Fact]
        public async Task GetMyLists_WithException_ShouldReturnInternalServerError()
        {
            // Arrange
            var userId = 1;
            SetupUserClaims(userId);
            _mockVocabularyListService
                .Setup(s => s.GetListsByUserAsync(userId, null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetMyLists(null);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetMyAndStaffLists_WithValidUserId_ShouldReturnLists()
        {
            // Arrange
            var userId = 1;
            var lists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "My List" },
                new VocabularyListDTO { VocabularyListId = 2, Name = "Staff List" }
            };

            SetupUserClaims(userId);
            _mockVocabularyListService
                .Setup(s => s.GetMyAndStaffListsAsync(userId, null))
                .ReturnsAsync(lists);

            // Act
            var result = await _controller.GetMyAndStaffLists(null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultLists = Assert.IsAssignableFrom<IEnumerable<VocabularyListDTO>>(okResult.Value);
            Assert.Equal(2, resultLists.Count());
            _mockVocabularyListService.Verify(s => s.GetMyAndStaffListsAsync(userId, null), Times.Once);
        }

        [Fact]
        public async Task GetMyAndStaffLists_WithSearchTerm_ShouldPassSearchTerm()
        {
            // Arrange
            var userId = 1;
            var searchTerm = "test";
            var lists = new List<VocabularyListDTO>
            {
                new VocabularyListDTO { VocabularyListId = 1, Name = "Test List" }
            };

            SetupUserClaims(userId);
            _mockVocabularyListService
                .Setup(s => s.GetMyAndStaffListsAsync(userId, searchTerm))
                .ReturnsAsync(lists);

            // Act
            var result = await _controller.GetMyAndStaffLists(searchTerm);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockVocabularyListService.Verify(s => s.GetMyAndStaffListsAsync(userId, searchTerm), Times.Once);
        }

        [Fact]
        public async Task GetMyAndStaffLists_WithNullUserIdClaim_ShouldReturnUnauthorized()
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
            var result = await _controller.GetMyAndStaffLists(null);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Contains("Invalid token", errorResponse.Error);
        }

        [Fact]
        public async Task GetMyAndStaffLists_WithException_ShouldReturnInternalServerError()
        {
            // Arrange
            var userId = 1;
            SetupUserClaims(userId);
            _mockVocabularyListService
                .Setup(s => s.GetMyAndStaffListsAsync(userId, null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetMyAndStaffLists(null);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
    }
}

