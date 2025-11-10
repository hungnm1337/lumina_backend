using DataLayer.DTOs;
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
    public class CreateVocabularyTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITextToSpeechService> _mockTtsService;
        private readonly Mock<IVocabularyRepository> _mockVocabularyRepository;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly VocabulariesController _controller;

        public CreateVocabularyTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTtsService = new Mock<ITextToSpeechService>();
            _mockVocabularyRepository = new Mock<IVocabularyRepository>();
            _mockVocabularyListRepository = new Mock<IVocabularyListRepository>();

            _mockUnitOfWork.Setup(u => u.Vocabularies).Returns(_mockVocabularyRepository.Object);
            _mockUnitOfWork.Setup(u => u.VocabularyLists).Returns(_mockVocabularyListRepository.Object);

            _controller = new VocabulariesController(_mockUnitOfWork.Object, _mockTtsService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task Create_WithValidInputAndAudioGeneration_ShouldReturn201Created()
        {
            // Arrange
            var request = new VocabulariesController.CreateVocabularyRequest
            {
                VocabularyListId = 1,
                Word = "Hello",
                TypeOfWord = "noun",
                Category = "greeting",
                Definition = "Xin chào",
                Example = "Hello, how are you?",
                GenerateAudio = true
            };
            var userId = 1;
            var vocabularyList = new VocabularyList { VocabularyListId = 1, Name = "Test List" };
            var audioResult = new UploadResultDTO { Url = "https://example.com/audio.mp3", PublicId = "audio123" };
            var createdVocabulary = new Vocabulary { VocabularyId = 1, Word = "Hello" };

            SetupUserClaims(userId);
            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(vocabularyList);
            _mockTtsService.Setup(s => s.GenerateAudioAsync("Hello", "en-US")).ReturnsAsync(audioResult);
            _mockVocabularyRepository.Setup(r => r.AddAsync(It.IsAny<Vocabulary>())).Callback<Vocabulary>(v => createdVocabulary = v);
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(StatusCodes.Status201Created, createdAtResult.StatusCode);
            Assert.Equal(nameof(VocabulariesController.GetList), createdAtResult.ActionName);
            _mockVocabularyListRepository.Verify(r => r.FindByIdAsync(1), Times.Once);
            _mockTtsService.Verify(s => s.GenerateAudioAsync("Hello", "en-US"), Times.Once);
            _mockVocabularyRepository.Verify(r => r.AddAsync(It.IsAny<Vocabulary>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task Create_WithValidInputWithoutAudioGeneration_ShouldReturn201Created()
        {
            // Arrange
            var request = new VocabulariesController.CreateVocabularyRequest
            {
                VocabularyListId = 1,
                Word = "Hello",
                TypeOfWord = "noun",
                Definition = "Xin chào",
                GenerateAudio = false
            };
            var userId = 1;
            var vocabularyList = new VocabularyList { VocabularyListId = 1, Name = "Test List" };

            SetupUserClaims(userId);
            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(vocabularyList);
            _mockVocabularyRepository.Setup(r => r.AddAsync(It.IsAny<Vocabulary>()));
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(StatusCodes.Status201Created, createdAtResult.StatusCode);
            _mockTtsService.Verify(s => s.GenerateAudioAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Create_WithEmptyWord_ShouldSkipAudioGeneration()
        {
            // Arrange
            var request = new VocabulariesController.CreateVocabularyRequest
            {
                VocabularyListId = 1,
                Word = "",
                TypeOfWord = "noun",
                Definition = "Xin chào",
                GenerateAudio = true
            };
            var userId = 1;
            var vocabularyList = new VocabularyList { VocabularyListId = 1, Name = "Test List" };

            SetupUserClaims(userId);
            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(vocabularyList);
            _mockVocabularyRepository.Setup(r => r.AddAsync(It.IsAny<Vocabulary>()));
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(StatusCodes.Status201Created, createdAtResult.StatusCode);
            _mockTtsService.Verify(s => s.GenerateAudioAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Create_WhenAudioGenerationFails_ShouldStillCreateVocabulary()
        {
            // Arrange
            var request = new VocabulariesController.CreateVocabularyRequest
            {
                VocabularyListId = 1,
                Word = "Hello",
                TypeOfWord = "noun",
                Definition = "Xin chào",
                GenerateAudio = true
            };
            var userId = 1;
            var vocabularyList = new VocabularyList { VocabularyListId = 1, Name = "Test List" };

            SetupUserClaims(userId);
            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(vocabularyList);
            _mockTtsService.Setup(s => s.GenerateAudioAsync("Hello", "en-US")).ThrowsAsync(new Exception("Audio generation failed"));
            _mockVocabularyRepository.Setup(r => r.AddAsync(It.IsAny<Vocabulary>()));
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(StatusCodes.Status201Created, createdAtResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.AddAsync(It.IsAny<Vocabulary>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        #endregion

        #region ModelState Validation Tests

        [Fact]
        public async Task Create_WithInvalidModelState_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new VocabulariesController.CreateVocabularyRequest
            {
                VocabularyListId = 1,
                Word = "",
                TypeOfWord = "",
                Definition = ""
            };
            _controller.ModelState.AddModelError("Word", "Word is required");

            // Act
            var result = await _controller.Create(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task Create_WithNullUserIdClaim_ShouldReturn401Unauthorized()
        {
            // Arrange
            var request = new VocabulariesController.CreateVocabularyRequest
            {
                VocabularyListId = 1,
                Word = "Hello",
                TypeOfWord = "noun",
                Definition = "Xin chào"
            };
            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.Create(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid token - User ID could not be determined.", errorResponse.Error);
        }

        [Fact]
        public async Task Create_WithInvalidUserIdClaim_ShouldReturn401Unauthorized()
        {
            // Arrange
            var request = new VocabulariesController.CreateVocabularyRequest
            {
                VocabularyListId = 1,
                Word = "Hello",
                TypeOfWord = "noun",
                Definition = "Xin chào"
            };
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
            var result = await _controller.Create(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid token - User ID could not be determined.", errorResponse.Error);
        }

        #endregion

        #region VocabularyList Not Found Tests

        [Fact]
        public async Task Create_WithNonExistentVocabularyList_ShouldReturn404NotFound()
        {
            // Arrange
            var request = new VocabulariesController.CreateVocabularyRequest
            {
                VocabularyListId = 999,
                Word = "Hello",
                TypeOfWord = "noun",
                Definition = "Xin chào"
            };
            var userId = 1;

            SetupUserClaims(userId);
            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((VocabularyList?)null);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Equal("Vocabulary list not found.", errorResponse.Error);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task Create_WhenRepositoryThrowsException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var request = new VocabulariesController.CreateVocabularyRequest
            {
                VocabularyListId = 1,
                Word = "Hello",
                TypeOfWord = "noun",
                Definition = "Xin chào"
            };
            var userId = 1;
            var vocabularyList = new VocabularyList { VocabularyListId = 1, Name = "Test List" };

            SetupUserClaims(userId);
            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(vocabularyList);
            _mockVocabularyRepository.Setup(r => r.AddAsync(It.IsAny<Vocabulary>())).ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Create(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("An internal server error occurred.", errorResponse.Error);
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

