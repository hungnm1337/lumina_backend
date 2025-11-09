using DataLayer.DTOs.Auth;
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
    public class GetPublicVocabularyTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITextToSpeechService> _mockTtsService;
        private readonly Mock<IVocabularyRepository> _mockVocabularyRepository;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly VocabulariesController _controller;

        public GetPublicVocabularyTests()
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
        public async Task GetPublicVocabulary_WithPublishedList_ShouldReturn200Ok()
        {
            // Arrange
            var listId = 1;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Public List",
                Status = "Published"
            };
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    VocabularyId = 1,
                    VocabularyListId = listId,
                    Word = "Hello",
                    TypeOfWord = "noun",
                    Category = "greeting",
                    Definition = "Xin chào",
                    Example = "Hello, how are you?"
                },
                new Vocabulary
                {
                    VocabularyId = 2,
                    VocabularyListId = listId,
                    Word = "World",
                    TypeOfWord = "noun",
                    Definition = "Thế giới"
                }
            };

            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);
            _mockVocabularyRepository.Setup(r => r.GetByListAsync(listId, null)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetPublicVocabulary(listId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyListRepository.Verify(r => r.FindByIdAsync(listId), Times.Once);
            _mockVocabularyRepository.Verify(r => r.GetByListAsync(listId, null), Times.Once);
        }

        [Fact]
        public async Task GetPublicVocabulary_ShouldReturnCorrectFormat()
        {
            // Arrange
            var listId = 1;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Public List",
                Status = "Published"
            };
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    VocabularyId = 1,
                    VocabularyListId = listId,
                    Word = "Hello",
                    TypeOfWord = "noun",
                    Category = "greeting",
                    Definition = "Xin chào",
                    Example = "Hello, how are you?"
                }
            };

            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);
            _mockVocabularyRepository.Setup(r => r.GetByListAsync(listId, null)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetPublicVocabulary(listId);

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
            Assert.True(itemProperties.Any(p => p.Name == "word"));
            Assert.True(itemProperties.Any(p => p.Name == "definition"));
            Assert.True(itemProperties.Any(p => p.Name == "category"));
            Assert.True(itemProperties.Any(p => p.Name == "example"));
            Assert.True(itemProperties.Any(p => p.Name == "audioUrl"));
        }

        [Fact]
        public async Task GetPublicVocabulary_WithEmptyList_ShouldReturn200Ok()
        {
            // Arrange
            var listId = 1;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Public List",
                Status = "Published"
            };
            var vocabularies = new List<Vocabulary>();

            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);
            _mockVocabularyRepository.Setup(r => r.GetByListAsync(listId, null)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetPublicVocabulary(listId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var resultValue = okResult.Value as IEnumerable<object>;
            Assert.NotNull(resultValue);
            Assert.Empty(resultValue);
        }

        [Fact]
        public async Task GetPublicVocabulary_WithNullOptionalFields_ShouldReturn200Ok()
        {
            // Arrange
            var listId = 1;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Public List",
                Status = "Published"
            };
            var vocabularies = new List<Vocabulary>
            {
                new Vocabulary
                {
                    VocabularyId = 1,
                    VocabularyListId = listId,
                    Word = "Hello",
                    TypeOfWord = "noun",
                    Category = null,
                    Definition = "Xin chào",
                    Example = null
                }
            };

            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);
            _mockVocabularyRepository.Setup(r => r.GetByListAsync(listId, null)).ReturnsAsync(vocabularies);

            // Act
            var result = await _controller.GetPublicVocabulary(listId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        }

        #endregion

        #region VocabularyList Not Found Tests

        [Fact]
        public async Task GetPublicVocabulary_WithNonExistentListId_ShouldReturn404NotFound()
        {
            // Arrange
            var listId = 999;

            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(listId)).ReturnsAsync((VocabularyList?)null);

            // Act
            var result = await _controller.GetPublicVocabulary(listId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Equal("Vocabulary list not found or not published.", errorResponse.Error);
            _mockVocabularyListRepository.Verify(r => r.FindByIdAsync(listId), Times.Once);
            _mockVocabularyRepository.Verify(r => r.GetByListAsync(It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task GetPublicVocabulary_WithUnpublishedList_ShouldReturn404NotFound()
        {
            // Arrange
            var listId = 1;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Private List",
                Status = "Draft" // Not published
            };

            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);

            // Act
            var result = await _controller.GetPublicVocabulary(listId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Equal("Vocabulary list not found or not published.", errorResponse.Error);
            _mockVocabularyRepository.Verify(r => r.GetByListAsync(It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task GetPublicVocabulary_WithNullStatus_ShouldReturn404NotFound()
        {
            // Arrange
            var listId = 1;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "List",
                Status = null
            };

            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);

            // Act
            var result = await _controller.GetPublicVocabulary(listId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task GetPublicVocabulary_WhenFindByIdThrowsException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var listId = 1;

            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(listId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetPublicVocabulary(listId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("An internal server error occurred.", errorResponse.Error);
        }

        [Fact]
        public async Task GetPublicVocabulary_WhenGetByListThrowsException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var listId = 1;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Public List",
                Status = "Published"
            };

            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);
            _mockVocabularyRepository.Setup(r => r.GetByListAsync(listId, null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetPublicVocabulary(listId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("An internal server error occurred.", errorResponse.Error);
        }

        #endregion

        #region Boundary Cases Tests

        [Fact]
        public async Task GetPublicVocabulary_WithZeroListId_ShouldReturn404NotFound()
        {
            // Arrange
            var listId = 0;

            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(listId)).ReturnsAsync((VocabularyList?)null);

            // Act
            var result = await _controller.GetPublicVocabulary(listId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task GetPublicVocabulary_WithNegativeListId_ShouldReturn404NotFound()
        {
            // Arrange
            var listId = -1;

            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(listId)).ReturnsAsync((VocabularyList?)null);

            // Act
            var result = await _controller.GetPublicVocabulary(listId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        #endregion
    }
}

