using DataLayer.Models;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.TextToSpeech;

namespace Lumina.Tests
{
    public class SearchVocabularyTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITextToSpeechService> _mockTtsService;
        private readonly Mock<IVocabularyRepository> _mockVocabularyRepository;
        private readonly VocabulariesController _controller;

        public SearchVocabularyTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTtsService = new Mock<ITextToSpeechService>();
            _mockVocabularyRepository = new Mock<IVocabularyRepository>();

            _mockUnitOfWork.Setup(u => u.Vocabularies).Returns(_mockVocabularyRepository.Object);

            _controller = new VocabulariesController(_mockUnitOfWork.Object, _mockTtsService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task Search_WithValidTermAndListId_ShouldReturn200Ok()
        {
            // Arrange
            var term = "Hello";
            int? listId = 1;
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
                }
            };

            _mockVocabularyRepository.Setup(r => r.SearchAsync(term, listId)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.Search(term, listId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.SearchAsync(term, listId), Times.Once);
        }

        [Fact]
        public async Task Search_WithValidTermWithoutListId_ShouldReturn200Ok()
        {
            // Arrange
            var term = "Hello";
            int? listId = null;
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    VocabularyId = 1,
                    VocabularyListId = 1,
                    Word = "Hello",
                    TypeOfWord = "noun",
                    Definition = "Xin chào"
                }
            };

            _mockVocabularyRepository.Setup(r => r.SearchAsync(term, listId)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.Search(term, listId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.SearchAsync(term, listId), Times.Once);
        }

        [Fact]
        public async Task Search_WithEmptyResults_ShouldReturn200Ok()
        {
            // Arrange
            var term = "NonExistentWord";
            int? listId = null;
            var vocabularies = new List<Vocabulary>();

            _mockVocabularyRepository.Setup(r => r.SearchAsync(term, listId)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.Search(term, listId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var resultValue = okResult.Value as IEnumerable<object>;
            Assert.NotNull(resultValue);
            Assert.Empty(resultValue);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task Search_WithNullTerm_ShouldReturn400BadRequest()
        {
            // Arrange
            string? term = null;
            int? listId = null;

            // Act
            var result = await _controller.Search(term, listId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task Search_WithEmptyTerm_ShouldReturn400BadRequest()
        {
            // Arrange
            var term = string.Empty;
            int? listId = null;

            // Act
            var result = await _controller.Search(term, listId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task Search_WithWhitespaceTerm_ShouldReturn400BadRequest()
        {
            // Arrange
            var term = "   ";
            int? listId = null;

            // Act
            var result = await _controller.Search(term, listId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task Search_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            var term = "Hello";
            int? listId = null;

            _mockVocabularyRepository.Setup(r => r.SearchAsync(term, listId)).ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () => await _controller.Search(term, listId));
            _mockVocabularyRepository.Verify(r => r.SearchAsync(term, listId), Times.Once);
        }

        #endregion
    }
}

