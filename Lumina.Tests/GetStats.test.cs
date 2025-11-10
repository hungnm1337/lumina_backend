using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.TextToSpeech;
using System.Linq;

namespace Lumina.Tests
{
    public class GetStatsTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITextToSpeechService> _mockTtsService;
        private readonly Mock<IVocabularyRepository> _mockVocabularyRepository;
        private readonly VocabulariesController _controller;

        public GetStatsTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTtsService = new Mock<ITextToSpeechService>();
            _mockVocabularyRepository = new Mock<IVocabularyRepository>();

            _mockUnitOfWork.Setup(u => u.Vocabularies).Returns(_mockVocabularyRepository.Object);

            _controller = new VocabulariesController(_mockUnitOfWork.Object, _mockTtsService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task GetStats_WithValidData_ShouldReturn200Ok()
        {
            // Arrange
            var countsByList = new Dictionary<int, int>
            {
                { 1, 10 },
                { 2, 5 },
                { 3, 15 }
            };
            var totalCount = 30;

            _mockVocabularyRepository.Setup(r => r.GetCountsByListAsync()).ReturnsAsync(countsByList);
            _mockVocabularyRepository.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(totalCount);

            // Act
            var result = await _controller.GetStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.GetCountsByListAsync(), Times.Once);
            _mockVocabularyRepository.Verify(r => r.GetTotalCountAsync(), Times.Once);
            
            // Force enumeration of countsByList to ensure the Select() is executed (line 376)
            var resultProperties = okResult.Value.GetType().GetProperties();
            var countsByListProperty = resultProperties.First(p => p.Name == "countsByList");
            var countsByListValue = countsByListProperty.GetValue(okResult.Value);
            var enumerable = countsByListValue as System.Collections.IEnumerable;
            Assert.NotNull(enumerable);
            // Enumerate to trigger the Select() execution
            foreach (var item in enumerable)
            {
                // Just enumerate to trigger execution
            }
        }

        [Fact]
        public async Task GetStats_ShouldReturnCorrectFormat()
        {
            // Arrange
            var countsByList = new Dictionary<int, int>
            {
                { 1, 10 },
                { 2, 5 }
            };
            var totalCount = 15;

            _mockVocabularyRepository.Setup(r => r.GetCountsByListAsync()).ReturnsAsync(countsByList);
            _mockVocabularyRepository.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(totalCount);

            // Act
            var result = await _controller.GetStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.NotNull(okResult.Value);
            // Verify the result has the expected properties
            var resultProperties = okResult.Value.GetType().GetProperties();
            Assert.True(resultProperties.Any(p => p.Name == "totalCount"));
            Assert.True(resultProperties.Any(p => p.Name == "countsByList"));
            
            // Force enumeration of countsByList to ensure the Select() is executed (line 376)
            var countsByListProperty = resultProperties.First(p => p.Name == "countsByList");
            var countsByListValue = countsByListProperty.GetValue(okResult.Value);
            Assert.NotNull(countsByListValue);
            var enumerable = countsByListValue as System.Collections.IEnumerable;
            Assert.NotNull(enumerable);
            // Enumerate to trigger the Select() execution
            var list = enumerable.Cast<object>().ToList();
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public async Task GetStats_WithEmptyCounts_ShouldReturn200Ok()
        {
            // Arrange
            var countsByList = new Dictionary<int, int>();
            var totalCount = 0;

            _mockVocabularyRepository.Setup(r => r.GetCountsByListAsync()).ReturnsAsync(countsByList);
            _mockVocabularyRepository.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(totalCount);

            // Act
            var result = await _controller.GetStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.NotNull(okResult.Value);
            // Verify the result has the expected properties
            var resultProperties = okResult.Value.GetType().GetProperties();
            Assert.True(resultProperties.Any(p => p.Name == "totalCount"));
            Assert.True(resultProperties.Any(p => p.Name == "countsByList"));
        }

        [Fact]
        public async Task GetStats_WithSingleList_ShouldReturn200Ok()
        {
            // Arrange
            var countsByList = new Dictionary<int, int>
            {
                { 1, 20 }
            };
            var totalCount = 20;

            _mockVocabularyRepository.Setup(r => r.GetCountsByListAsync()).ReturnsAsync(countsByList);
            _mockVocabularyRepository.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(totalCount);

            // Act
            var result = await _controller.GetStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.NotNull(okResult.Value);
            // Verify the result has the expected properties
            var resultProperties = okResult.Value.GetType().GetProperties();
            Assert.True(resultProperties.Any(p => p.Name == "totalCount"));
            Assert.True(resultProperties.Any(p => p.Name == "countsByList"));
            
            // Force enumeration of countsByList to ensure the Select() is executed (line 376)
            var countsByListProperty = resultProperties.First(p => p.Name == "countsByList");
            var countsByListValue = countsByListProperty.GetValue(okResult.Value);
            var enumerable = countsByListValue as System.Collections.IEnumerable;
            Assert.NotNull(enumerable);
            // Enumerate to trigger the Select() execution
            var list = enumerable.Cast<object>().ToList();
            Assert.Single(list);
        }

        [Fact]
        public async Task GetStats_ShouldReturnCorrectCountsByList()
        {
            // Arrange
            var countsByList = new Dictionary<int, int>
            {
                { 1, 10 },
                { 2, 5 },
                { 3, 15 }
            };
            var totalCount = 30;

            _mockVocabularyRepository.Setup(r => r.GetCountsByListAsync()).ReturnsAsync(countsByList);
            _mockVocabularyRepository.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(totalCount);

            // Act
            var result = await _controller.GetStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.NotNull(okResult.Value);
            // Verify the result has the expected properties
            var resultProperties = okResult.Value.GetType().GetProperties();
            Assert.True(resultProperties.Any(p => p.Name == "totalCount"));
            Assert.True(resultProperties.Any(p => p.Name == "countsByList"));
            
            // Force enumeration of countsByList to ensure the Select() is executed
            var countsByListProperty = resultProperties.First(p => p.Name == "countsByList");
            var countsByListValue = countsByListProperty.GetValue(okResult.Value);
            Assert.NotNull(countsByListValue);
            var enumerable = countsByListValue as System.Collections.IEnumerable;
            Assert.NotNull(enumerable);
            // Enumerate to trigger the Select() execution
            var count = 0;
            foreach (var item in enumerable)
            {
                count++;
                Assert.NotNull(item);
            }
            Assert.Equal(3, count);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task GetStats_WhenGetCountsByListThrowsException_ShouldThrowException()
        {
            // Arrange
            _mockVocabularyRepository.Setup(r => r.GetCountsByListAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () => await _controller.GetStats());
            _mockVocabularyRepository.Verify(r => r.GetCountsByListAsync(), Times.Once);
        }

        [Fact]
        public async Task GetStats_WhenGetTotalCountThrowsException_ShouldThrowException()
        {
            // Arrange
            var countsByList = new Dictionary<int, int> { { 1, 10 } };
            _mockVocabularyRepository.Setup(r => r.GetCountsByListAsync()).ReturnsAsync(countsByList);
            _mockVocabularyRepository.Setup(r => r.GetTotalCountAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () => await _controller.GetStats());
            _mockVocabularyRepository.Verify(r => r.GetCountsByListAsync(), Times.Once);
            _mockVocabularyRepository.Verify(r => r.GetTotalCountAsync(), Times.Once);
        }

        #endregion
    }
}

