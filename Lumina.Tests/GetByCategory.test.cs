using DataLayer.Models;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.TextToSpeech;
using System.Linq;

namespace Lumina.Tests
{
    public class GetByCategoryTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITextToSpeechService> _mockTtsService;
        private readonly Mock<IVocabularyRepository> _mockVocabularyRepository;
        private readonly VocabulariesController _controller;

        public GetByCategoryTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTtsService = new Mock<ITextToSpeechService>();
            _mockVocabularyRepository = new Mock<IVocabularyRepository>();

            _mockUnitOfWork.Setup(u => u.Vocabularies).Returns(_mockVocabularyRepository.Object);

            _controller = new VocabulariesController(_mockUnitOfWork.Object, _mockTtsService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task GetByCategory_WithValidCategory_ShouldReturn200Ok()
        {
            // Arrange
            var category = "greeting";
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
                    Word = "Hi",
                    TypeOfWord = "noun",
                    Category = "greeting",
                    Definition = "Chào"
                }
            };

            _mockVocabularyRepository.Setup(r => r.GetByCategoryAsync(category)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetByCategory(category);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.GetByCategoryAsync(category), Times.Once);
        }

        [Fact]
        public async Task GetByCategory_WithEmptyResults_ShouldReturn200Ok()
        {
            // Arrange
            var category = "nonexistent";
            var vocabularies = new List<Vocabulary>();

            _mockVocabularyRepository.Setup(r => r.GetByCategoryAsync(category)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetByCategory(category);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var resultValue = okResult.Value as IEnumerable<object>;
            Assert.NotNull(resultValue);
            Assert.Empty(resultValue);
            _mockVocabularyRepository.Verify(r => r.GetByCategoryAsync(category), Times.Once);
        }

        [Fact]
        public async Task GetByCategory_WithSingleResult_ShouldReturn200Ok()
        {
            // Arrange
            var category = "food";
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    VocabularyId = 1,
                    VocabularyListId = 1,
                    Word = "Apple",
                    TypeOfWord = "noun",
                    Category = "food",
                    Definition = "Táo"
                }
            };

            _mockVocabularyRepository.Setup(r => r.GetByCategoryAsync(category)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetByCategory(category);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.GetByCategoryAsync(category), Times.Once);
        }

        [Fact]
        public async Task GetByCategory_ShouldReturnCorrectFormat()
        {
            // Arrange
            var category = "greeting";
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    VocabularyId = 1,
                    VocabularyListId = 5,
                    Word = "Hello",
                    TypeOfWord = "noun",
                    Category = "greeting",
                    Definition = "Xin chào",
                    Example = "Hello, how are you?"
                }
            };

            _mockVocabularyRepository.Setup(r => r.GetByCategoryAsync(category)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetByCategory(category);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.NotNull(okResult.Value);
            // Verify the result is an enumerable collection
            var resultValue = okResult.Value as IEnumerable<object>;
            Assert.NotNull(resultValue);
            var itemsList = resultValue.ToList();
            Assert.Single(itemsList);
            // Verify the item has the expected properties using reflection
            var firstItem = itemsList.First();
            var itemProperties = firstItem.GetType().GetProperties();
            Assert.True(itemProperties.Any(p => p.Name == "id"));
            Assert.True(itemProperties.Any(p => p.Name == "listId"));
            Assert.True(itemProperties.Any(p => p.Name == "word"));
            Assert.True(itemProperties.Any(p => p.Name == "type"));
            Assert.True(itemProperties.Any(p => p.Name == "category"));
            Assert.True(itemProperties.Any(p => p.Name == "definition"));
            Assert.True(itemProperties.Any(p => p.Name == "example"));
        }

        #endregion

        #region Boundary Cases Tests

        [Fact]
        public async Task GetByCategory_WithNullCategory_ShouldReturn200Ok()
        {
            // Arrange
            string? category = null;
            var vocabularies = new List<Vocabulary>();

            _mockVocabularyRepository.Setup(r => r.GetByCategoryAsync(category!)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetByCategory(category!);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.GetByCategoryAsync(category!), Times.Once);
        }

        [Fact]
        public async Task GetByCategory_WithEmptyCategory_ShouldReturn200Ok()
        {
            // Arrange
            var category = string.Empty;
            var vocabularies = new List<Vocabulary>();

            _mockVocabularyRepository.Setup(r => r.GetByCategoryAsync(category)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetByCategory(category);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.GetByCategoryAsync(category), Times.Once);
        }

        [Fact]
        public async Task GetByCategory_WithWhitespaceCategory_ShouldReturn200Ok()
        {
            // Arrange
            var category = "   ";
            var vocabularies = new List<Vocabulary>();

            _mockVocabularyRepository.Setup(r => r.GetByCategoryAsync(category)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetByCategory(category);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.GetByCategoryAsync(category), Times.Once);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task GetByCategory_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            var category = "greeting";

            _mockVocabularyRepository.Setup(r => r.GetByCategoryAsync(category))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () => await _controller.GetByCategory(category));
            _mockVocabularyRepository.Verify(r => r.GetByCategoryAsync(category), Times.Once);
        }

        #endregion
    }
}

