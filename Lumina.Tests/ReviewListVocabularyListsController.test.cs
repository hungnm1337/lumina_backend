using DataLayer.DTOs.Auth;
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
    public class ReviewListVocabularyListsControllerTests
    {
        private readonly Mock<IVocabularyListService> _mockVocabularyListService;
        private readonly Mock<ILogger<VocabularyListsController>> _mockLogger;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly VocabularyListsController _controller;

        public ReviewListVocabularyListsControllerTests()
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
        public async Task ReviewList_WithApproval_ShouldReturnNoContent()
        {
            // Arrange
            var userId = 2;
            var listId = 1;
            var request = new VocabularyListReviewRequest
            {
                IsApproved = true,
                Comment = "Good list"
            };

            SetupUserClaims(userId);
            _mockVocabularyListService
                .Setup(s => s.ReviewListAsync(listId, true, request.Comment, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ReviewList(listId, request);

            // Assert
            var noContentResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(204, noContentResult.StatusCode);
            _mockVocabularyListService.Verify(s => s.ReviewListAsync(listId, true, request.Comment, userId), Times.Once);
        }

        [Fact]
        public async Task ReviewList_WithRejection_ShouldReturnNoContent()
        {
            // Arrange
            var userId = 2;
            var listId = 1;
            var request = new VocabularyListReviewRequest
            {
                IsApproved = false,
                Comment = "Needs improvement"
            };

            SetupUserClaims(userId);
            _mockVocabularyListService
                .Setup(s => s.ReviewListAsync(listId, false, request.Comment, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ReviewList(listId, request);

            // Assert
            var noContentResult = Assert.IsType<NoContentResult>(result);
            _mockVocabularyListService.Verify(s => s.ReviewListAsync(listId, false, request.Comment, userId), Times.Once);
        }

        [Fact]
        public async Task ReviewList_WithNullComment_ShouldReturnNoContent()
        {
            // Arrange
            var userId = 2;
            var listId = 1;
            var request = new VocabularyListReviewRequest
            {
                IsApproved = true,
                Comment = null
            };

            SetupUserClaims(userId);
            _mockVocabularyListService
                .Setup(s => s.ReviewListAsync(listId, true, null, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ReviewList(listId, request);

            // Assert
            var noContentResult = Assert.IsType<NoContentResult>(result);
            _mockVocabularyListService.Verify(s => s.ReviewListAsync(listId, true, null, userId), Times.Once);
        }

        [Fact]
        public async Task ReviewList_WithNullUserIdClaim_ShouldReturnUnauthorized()
        {
            // Arrange
            var listId = 1;
            var request = new VocabularyListReviewRequest { IsApproved = true };
            var claims = new List<Claim>();
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.ReviewList(listId, request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Contains("Invalid token", errorResponse.Error);
        }

        [Fact]
        public async Task ReviewList_WithServiceReturningFalse_ShouldReturnNotFound()
        {
            // Arrange
            var userId = 2;
            var listId = 999;
            var request = new VocabularyListReviewRequest { IsApproved = true };

            SetupUserClaims(userId);
            _mockVocabularyListService
                .Setup(s => s.ReviewListAsync(listId, true, null, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ReviewList(listId, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Contains("not found or is not pending review", errorResponse.Error);
        }

        [Fact]
        public async Task ReviewList_WithException_ShouldReturnInternalServerError()
        {
            // Arrange
            var userId = 2;
            var listId = 1;
            var request = new VocabularyListReviewRequest { IsApproved = true };

            SetupUserClaims(userId);
            _mockVocabularyListService
                .Setup(s => s.ReviewListAsync(listId, true, null, userId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.ReviewList(listId, request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
    }
}

