using DataLayer.Models;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.TextToSpeech;

namespace Lumina.Tests
{
    public class GetVocabularyByTypeTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITextToSpeechService> _mockTtsService;
        private readonly Mock<IVocabularyRepository> _mockVocabularyRepository;
        private readonly VocabulariesController _controller;

        public GetVocabularyByTypeTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTtsService = new Mock<ITextToSpeechService>();
            _mockVocabularyRepository = new Mock<IVocabularyRepository>();

            _mockUnitOfWork.Setup(u => u.Vocabularies).Returns(_mockVocabularyRepository.Object);

            _controller = new VocabulariesController(_mockUnitOfWork.Object, _mockTtsService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task GetByType_WithValidType_ShouldReturn200Ok()
        {
            // Arrange
            var type = "noun";
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    VocabularyId = 1,
                    VocabularyListId = 1,
                    Word = "Hello",
                    TypeOfWord = "noun",
                    Category = "greeting",
                    Definition = "Xin chào",
                    Example = "Hello, how are you?"
                },
                new Vocabulary
                {
                    VocabularyId = 2,
                    VocabularyListId = 1,
                    Word = "World",
                    TypeOfWord = "noun",
                    Definition = "Thế giới"
                }
            };

            _mockVocabularyRepository.Setup(r => r.GetByTypeAsync(type)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetByType(type);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.GetByTypeAsync(type), Times.Once);
        }

        [Fact]
        public async Task GetByType_WithEmptyResults_ShouldReturn200Ok()
        {
            // Arrange
            var type = "verb";
            var vocabularies = new List<Vocabulary>();

            _mockVocabularyRepository.Setup(r => r.GetByTypeAsync(type)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetByType(type);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var resultValue = okResult.Value as IEnumerable<object>;
            Assert.NotNull(resultValue);
            Assert.Empty(resultValue);
            _mockVocabularyRepository.Verify(r => r.GetByTypeAsync(type), Times.Once);
        }

        [Fact]
        public async Task GetByType_WithSingleResult_ShouldReturn200Ok()
        {
            // Arrange
            var type = "adjective";
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    VocabularyId = 1,
                    VocabularyListId = 1,
                    Word = "Beautiful",
                    TypeOfWord = "adjective",
                    Definition = "Đẹp"
                }
            };

            _mockVocabularyRepository.Setup(r => r.GetByTypeAsync(type)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetByType(type);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.GetByTypeAsync(type), Times.Once);
        }

        #endregion

        #region Boundary Cases Tests

        [Fact]
        public async Task GetByType_WithNullType_ShouldReturn200Ok()
        {
            // Arrange
            string? type = null;
            var vocabularies = new List<Vocabulary>();

            _mockVocabularyRepository.Setup(r => r.GetByTypeAsync(type!)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetByType(type!);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.GetByTypeAsync(type!), Times.Once);
        }

        [Fact]
        public async Task GetByType_WithEmptyType_ShouldReturn200Ok()
        {
            // Arrange
            var type = string.Empty;
            var vocabularies = new List<Vocabulary>();

            _mockVocabularyRepository.Setup(r => r.GetByTypeAsync(type)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetByType(type);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.GetByTypeAsync(type), Times.Once);
        }

        [Fact]
        public async Task GetByType_WithWhitespaceType_ShouldReturn200Ok()
        {
            // Arrange
            var type = "   ";
            var vocabularies = new List<Vocabulary>();

            _mockVocabularyRepository.Setup(r => r.GetByTypeAsync(type)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetByType(type);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.GetByTypeAsync(type), Times.Once);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task GetByType_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            var type = "noun";

            _mockVocabularyRepository.Setup(r => r.GetByTypeAsync(type)).ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () => await _controller.GetByType(type));
            _mockVocabularyRepository.Verify(r => r.GetByTypeAsync(type), Times.Once);
        }

        #endregion
    }
}

