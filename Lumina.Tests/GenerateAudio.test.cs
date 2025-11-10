using DataLayer.DTOs;
using DataLayer.Models;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.TextToSpeech;
using System.Linq;
using System.Security.Claims;

namespace Lumina.Tests
{
    public class GenerateAudioTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITextToSpeechService> _mockTtsService;
        private readonly Mock<IVocabularyRepository> _mockVocabularyRepository;
        private readonly VocabulariesController _controller;

        public GenerateAudioTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTtsService = new Mock<ITextToSpeechService>();
            _mockVocabularyRepository = new Mock<IVocabularyRepository>();

            _mockUnitOfWork.Setup(u => u.Vocabularies).Returns(_mockVocabularyRepository.Object);

            _controller = new VocabulariesController(_mockUnitOfWork.Object, _mockTtsService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task GenerateAudio_WithValidVocabularyId_ShouldReturn200Ok()
        {
            // Arrange
            var vocabularyId = 1;
            var vocabulary = new Vocabulary
            {
                VocabularyId = vocabularyId,
                Word = "Hello",
                TypeOfWord = "noun",
                Definition = "Xin chào"
            };
            var audioResult = new UploadResultDTO
            {
                Url = "https://example.com/audio.mp3",
                PublicId = "audio123"
            };

            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync(vocabulary);
            _mockTtsService.Setup(s => s.GenerateAudioAsync(vocabulary.Word, It.IsAny<string>())).ReturnsAsync(audioResult);
            _mockVocabularyRepository.Setup(r => r.UpdateAsync(It.IsAny<Vocabulary>()));
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _controller.GenerateAudio(vocabularyId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var resultValue = okResult.Value as dynamic;
            Assert.NotNull(resultValue);
            _mockVocabularyRepository.Verify(r => r.GetByIdAsync(vocabularyId), Times.Once);
            _mockTtsService.Verify(s => s.GenerateAudioAsync(vocabulary.Word, It.IsAny<string>()), Times.Once);
            _mockVocabularyRepository.Verify(r => r.UpdateAsync(It.IsAny<Vocabulary>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task GenerateAudio_WithValidVocabularyAndAudioUrl_ShouldReturnCorrectUrl()
        {
            // Arrange
            var vocabularyId = 1;
            var vocabulary = new Vocabulary
            {
                VocabularyId = vocabularyId,
                Word = "World",
                TypeOfWord = "noun",
                Definition = "Thế giới"
            };
            var audioResult = new UploadResultDTO
            {
                Url = "https://example.com/world.mp3",
                PublicId = "audio456"
            };

            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync(vocabulary);
            _mockTtsService.Setup(s => s.GenerateAudioAsync(vocabulary.Word, It.IsAny<string>())).ReturnsAsync(audioResult);
            _mockVocabularyRepository.Setup(r => r.UpdateAsync(It.IsAny<Vocabulary>()));
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _controller.GenerateAudio(vocabularyId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.NotNull(okResult.Value);
            // Verify that the response contains the expected data by checking the type
            var resultDict = okResult.Value.GetType().GetProperties();
            Assert.True(resultDict.Any(p => p.Name == "message"));
            Assert.True(resultDict.Any(p => p.Name == "audioUrl"));
        }

        #endregion

        #region Vocabulary Not Found Tests

        [Fact]
        public async Task GenerateAudio_WithNonExistentVocabularyId_ShouldReturn404NotFound()
        {
            // Arrange
            var vocabularyId = 999;

            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync((Vocabulary?)null);

            // Act
            var result = await _controller.GenerateAudio(vocabularyId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.GetByIdAsync(vocabularyId), Times.Once);
            _mockTtsService.Verify(s => s.GenerateAudioAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task GenerateAudio_WithEmptyWord_ShouldReturn400BadRequest()
        {
            // Arrange
            var vocabularyId = 1;
            var vocabulary = new Vocabulary
            {
                VocabularyId = vocabularyId,
                Word = "",
                TypeOfWord = "noun",
                Definition = "Empty word"
            };

            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync(vocabulary);

            // Act
            var result = await _controller.GenerateAudio(vocabularyId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            Assert.NotNull(badRequestResult.Value);
            // Verify message property exists
            var resultDict = badRequestResult.Value.GetType().GetProperties();
            Assert.True(resultDict.Any(p => p.Name == "message"));
            _mockTtsService.Verify(s => s.GenerateAudioAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GenerateAudio_WithWhitespaceWord_ShouldReturn400BadRequest()
        {
            // Arrange
            var vocabularyId = 1;
            var vocabulary = new Vocabulary
            {
                VocabularyId = vocabularyId,
                Word = "   ",
                TypeOfWord = "noun",
                Definition = "Whitespace word"
            };

            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync(vocabulary);

            // Act
            var result = await _controller.GenerateAudio(vocabularyId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            _mockTtsService.Verify(s => s.GenerateAudioAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GenerateAudio_WithNullWord_ShouldReturn400BadRequest()
        {
            // Arrange
            var vocabularyId = 1;
            var vocabulary = new Vocabulary
            {
                VocabularyId = vocabularyId,
                Word = null!,
                TypeOfWord = "noun",
                Definition = "Null word"
            };

            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync(vocabulary);

            // Act
            var result = await _controller.GenerateAudio(vocabularyId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            _mockTtsService.Verify(s => s.GenerateAudioAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task GenerateAudio_WhenGetByIdThrowsException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var vocabularyId = 1;

            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId))
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            var result = await _controller.GenerateAudio(vocabularyId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.NotNull(statusCodeResult.Value);
            // Verify message property exists
            var resultDict = statusCodeResult.Value.GetType().GetProperties();
            Assert.True(resultDict.Any(p => p.Name == "message"));
        }

        [Fact]
        public async Task GenerateAudio_WhenTtsServiceThrowsException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var vocabularyId = 1;
            var vocabulary = new Vocabulary
            {
                VocabularyId = vocabularyId,
                Word = "Hello",
                TypeOfWord = "noun",
                Definition = "Xin chào"
            };

            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync(vocabulary);
            _mockTtsService.Setup(s => s.GenerateAudioAsync(vocabulary.Word, It.IsAny<string>()))
                .ThrowsAsync(new Exception("TTS service error"));

            // Act
            var result = await _controller.GenerateAudio(vocabularyId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.NotNull(statusCodeResult.Value);
            // Verify message property exists
            var resultDict = statusCodeResult.Value.GetType().GetProperties();
            Assert.True(resultDict.Any(p => p.Name == "message"));
        }

        [Fact]
        public async Task GenerateAudio_WhenUpdateThrowsException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var vocabularyId = 1;
            var vocabulary = new Vocabulary
            {
                VocabularyId = vocabularyId,
                Word = "Hello",
                TypeOfWord = "noun",
                Definition = "Xin chào"
            };
            var audioResult = new UploadResultDTO
            {
                Url = "https://example.com/audio.mp3",
                PublicId = "audio123"
            };

            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync(vocabulary);
            _mockTtsService.Setup(s => s.GenerateAudioAsync(vocabulary.Word, It.IsAny<string>())).ReturnsAsync(audioResult);
            _mockVocabularyRepository.Setup(r => r.UpdateAsync(It.IsAny<Vocabulary>()))
                .ThrowsAsync(new Exception("Database update error"));

            // Act
            var result = await _controller.GenerateAudio(vocabularyId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.NotNull(statusCodeResult.Value);
            // Verify message property exists
            var resultDict = statusCodeResult.Value.GetType().GetProperties();
            Assert.True(resultDict.Any(p => p.Name == "message"));
        }

        #endregion

        #region Boundary Cases Tests

        [Fact]
        public async Task GenerateAudio_WithZeroVocabularyId_ShouldReturn404NotFound()
        {
            // Arrange
            var vocabularyId = 0;

            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync((Vocabulary?)null);

            // Act
            var result = await _controller.GenerateAudio(vocabularyId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task GenerateAudio_WithNegativeVocabularyId_ShouldReturn404NotFound()
        {
            // Arrange
            var vocabularyId = -1;

            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync((Vocabulary?)null);

            // Act
            var result = await _controller.GenerateAudio(vocabularyId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        #endregion
    }
}

