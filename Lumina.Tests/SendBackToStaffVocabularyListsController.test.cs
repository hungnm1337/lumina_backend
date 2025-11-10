using DataLayer.DTOs.Auth;
using DataLayer.Models;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Vocabulary;
using System.Security.Claims;
using Xunit;

namespace Lumina.Tests
{
    public class SendBackToStaffVocabularyListsControllerTests
    {
        private readonly Mock<IVocabularyListService> _mockVocabularyListService;
        private readonly Mock<ILogger<VocabularyListsController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly VocabularyListsController _controller;

        public SendBackToStaffVocabularyListsControllerTests()
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
        public async Task SendBackToStaff_WithRejectedList_ShouldReturnNoContent()
        {
            // Arrange
            var userId = 2;
            var listId = 1;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Status = "Rejected"
            };

            SetupUserClaims(userId);
            _mockUnitOfWork.Setup(u => u.VocabularyLists.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);
            _mockUnitOfWork.Setup(u => u.VocabularyLists.UpdateAsync(It.IsAny<VocabularyList>())).ReturnsAsync((VocabularyList l) => l);
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _controller.SendBackToStaff(listId);

            // Assert
            var noContentResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(204, noContentResult.StatusCode);
            Assert.Equal("Draft", vocabularyList.Status);
            Assert.Equal(userId, vocabularyList.UpdatedBy);
            Assert.NotNull(vocabularyList.UpdateAt);
            _mockUnitOfWork.Verify(u => u.VocabularyLists.UpdateAsync(It.IsAny<VocabularyList>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task SendBackToStaff_WithNullUserIdClaim_ShouldReturnUnauthorized()
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
            var result = await _controller.SendBackToStaff(listId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Contains("Invalid token", errorResponse.Error);
        }

        [Fact]
        public async Task SendBackToStaff_WithVocabularyListNotFound_ShouldReturnNotFound()
        {
            // Arrange
            var userId = 2;
            var listId = 999;
            SetupUserClaims(userId);
            _mockUnitOfWork.Setup(u => u.VocabularyLists.FindByIdAsync(listId)).ReturnsAsync((VocabularyList?)null);

            // Act
            var result = await _controller.SendBackToStaff(listId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Contains("not found", errorResponse.Error);
        }

        [Fact]
        public async Task SendBackToStaff_WithNonRejectedStatus_ShouldReturnBadRequest()
        {
            // Arrange
            var userId = 2;
            var listId = 1;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Status = "Published" // Not Rejected
            };

            SetupUserClaims(userId);
            _mockUnitOfWork.Setup(u => u.VocabularyLists.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);

            // Act
            var result = await _controller.SendBackToStaff(listId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Contains("Can only send back rejected", errorResponse.Error);
            _mockUnitOfWork.Verify(u => u.VocabularyLists.UpdateAsync(It.IsAny<VocabularyList>()), Times.Never);
        }

        [Fact]
        public async Task SendBackToStaff_WithException_ShouldReturnInternalServerError()
        {
            // Arrange
            var userId = 2;
            var listId = 1;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Status = "Rejected"
            };

            SetupUserClaims(userId);
            _mockUnitOfWork.Setup(u => u.VocabularyLists.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);
            _mockUnitOfWork.Setup(u => u.VocabularyLists.UpdateAsync(It.IsAny<VocabularyList>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.SendBackToStaff(listId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
    }
}

