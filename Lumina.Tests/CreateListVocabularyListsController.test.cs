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
using System.Security.Claims;
using Xunit;

namespace Lumina.Tests
{
    public class CreateListVocabularyListsControllerTests
    {
        private readonly Mock<IVocabularyListService> _mockVocabularyListService;
        private readonly Mock<ILogger<VocabularyListsController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly VocabularyListsController _controller;

        public CreateListVocabularyListsControllerTests()
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
        public async Task CreateList_WithValidRequest_ShouldReturnCreated()
        {
            // Arrange
            var userId = 1;
            var request = new VocabularyListCreateDTO
            {
                Name = "My Vocabulary List",
                IsPublic = true
            };
            var createdList = new VocabularyListDTO
            {
                VocabularyListId = 1,
                Name = "My Vocabulary List",
                IsPublic = true,
                Status = "Draft"
            };

            SetupUserClaims(userId);
            _mockVocabularyListService
                .Setup(s => s.CreateListAsync(request, userId))
                .ReturnsAsync(createdList);

            // Act
            var result = await _controller.CreateList(request);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(VocabularyListsController.GetListById), createdAtActionResult.ActionName);
            Assert.Equal(1, createdAtActionResult.RouteValues["id"]);
            Assert.Equal(createdList, createdAtActionResult.Value);
            _mockVocabularyListService.Verify(s => s.CreateListAsync(request, userId), Times.Once);
        }

        [Fact]
        public async Task CreateList_WithInvalidModelState_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new VocabularyListCreateDTO();
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.CreateList(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            _mockVocabularyListService.Verify(s => s.CreateListAsync(It.IsAny<VocabularyListCreateDTO>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task CreateList_WithNullUserIdClaim_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new VocabularyListCreateDTO { Name = "Test List" };
            var claims = new List<Claim>();
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.CreateList(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Contains("Invalid token", errorResponse.Error);
        }

        [Fact]
        public async Task CreateList_WithInvalidUserIdClaim_ShouldReturnUnauthorized()
        {
            // Arrange
            var request = new VocabularyListCreateDTO { Name = "Test List" };
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "invalid")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.CreateList(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Contains("Invalid token", errorResponse.Error);
        }

        [Fact]
        public async Task CreateList_WithException_ShouldReturnInternalServerError()
        {
            // Arrange
            var userId = 1;
            var request = new VocabularyListCreateDTO { Name = "Test List" };

            SetupUserClaims(userId);
            _mockVocabularyListService
                .Setup(s => s.CreateListAsync(request, userId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateList(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Contains("internal server error", errorResponse.Error);
        }
    }
}

