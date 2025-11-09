using DataLayer.DTOs.Auth;
using DataLayer.Models;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.TextToSpeech;
using System.Security.Claims;

namespace Lumina.Tests
{
    public class DeleteVocabularyTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITextToSpeechService> _mockTtsService;
        private readonly Mock<IVocabularyRepository> _mockVocabularyRepository;
        private readonly VocabulariesController _controller;

        public DeleteVocabularyTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTtsService = new Mock<ITextToSpeechService>();
            _mockVocabularyRepository = new Mock<IVocabularyRepository>();

            _mockUnitOfWork.Setup(u => u.Vocabularies).Returns(_mockVocabularyRepository.Object);

            _controller = new VocabulariesController(_mockUnitOfWork.Object, _mockTtsService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task Delete_WithValidVocabularyId_ShouldReturn200Ok()
        {
            // Arrange
            var vocabularyId = 1;
            var userId = 1;
            var existingVocabulary = new Vocabulary
            {
                VocabularyId = vocabularyId,
                Word = "Hello",
                TypeOfWord = "noun",
                Definition = "Xin chào"
            };

            SetupUserClaims(userId);
            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync(existingVocabulary);
            _mockVocabularyRepository.Setup(r => r.DeleteAsync(vocabularyId));
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _controller.Delete(vocabularyId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.GetByIdAsync(vocabularyId), Times.Once);
            _mockVocabularyRepository.Verify(r => r.DeleteAsync(vocabularyId), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task Delete_WithNullUserIdClaim_ShouldReturn401Unauthorized()
        {
            // Arrange
            var vocabularyId = 1;
            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.Delete(vocabularyId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid token - User ID could not be determined.", errorResponse.Error);
        }

        [Fact]
        public async Task Delete_WithInvalidUserIdClaim_ShouldReturn401Unauthorized()
        {
            // Arrange
            var vocabularyId = 1;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "invalid-number")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.Delete(vocabularyId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid token - User ID could not be determined.", errorResponse.Error);
        }

        #endregion

        #region Vocabulary Not Found Tests

        [Fact]
        public async Task Delete_WithNonExistentVocabularyId_ShouldReturn404NotFound()
        {
            // Arrange
            var vocabularyId = 999;
            var userId = 1;

            SetupUserClaims(userId);
            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync((Vocabulary?)null);

            // Act
            var result = await _controller.Delete(vocabularyId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task Delete_WhenRepositoryThrowsException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var vocabularyId = 1;
            var userId = 1;
            var existingVocabulary = new Vocabulary
            {
                VocabularyId = vocabularyId,
                Word = "Hello",
                TypeOfWord = "noun",
                Definition = "Xin chào"
            };

            SetupUserClaims(userId);
            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync(existingVocabulary);
            _mockVocabularyRepository.Setup(r => r.DeleteAsync(vocabularyId)).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Delete(vocabularyId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("An internal server error occurred.", errorResponse.Error);
        }

        [Fact]
        public async Task Delete_WhenGetByIdThrowsException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var vocabularyId = 1;
            var userId = 1;

            SetupUserClaims(userId);
            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ThrowsAsync(new Exception("Database connection error"));

            // Act
            var result = await _controller.Delete(vocabularyId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("An internal server error occurred.", errorResponse.Error);
        }

        #endregion

        #region Boundary Cases Tests

        [Fact]
        public async Task Delete_WithZeroVocabularyId_ShouldReturn404NotFound()
        {
            // Arrange
            var vocabularyId = 0;
            var userId = 1;

            SetupUserClaims(userId);
            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync((Vocabulary?)null);

            // Act
            var result = await _controller.Delete(vocabularyId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task Delete_WithNegativeVocabularyId_ShouldReturn404NotFound()
        {
            // Arrange
            var vocabularyId = -1;
            var userId = 1;

            SetupUserClaims(userId);
            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync((Vocabulary?)null);

            // Act
            var result = await _controller.Delete(vocabularyId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
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

