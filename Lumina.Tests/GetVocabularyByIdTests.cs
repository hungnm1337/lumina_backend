using DataLayer.Models;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.TextToSpeech;

namespace Lumina.Tests
{
    public class GetVocabularyByIdTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITextToSpeechService> _mockTtsService;
        private readonly Mock<IVocabularyRepository> _mockVocabularyRepository;
        private readonly VocabulariesController _controller;

        public GetVocabularyByIdTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTtsService = new Mock<ITextToSpeechService>();
            _mockVocabularyRepository = new Mock<IVocabularyRepository>();

            _mockUnitOfWork.Setup(u => u.Vocabularies).Returns(_mockVocabularyRepository.Object);

            _controller = new VocabulariesController(_mockUnitOfWork.Object, _mockTtsService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task GetById_WithValidVocabularyId_ShouldReturn200Ok()
        {
         
            var vocabularyId = 1;
            var vocabulary = new Vocabulary
            {
                VocabularyId = vocabularyId,
                VocabularyListId = 5,
                Word = "Hello",
                TypeOfWord = "noun",
                Category = "greeting",
                Definition = "Xin chào",
                Example = "Hello, how are you?"
            };

            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync(vocabulary);

          
            var result = await _controller.GetById(vocabularyId);

           
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            
            var resultValue = okResult.Value;
            Assert.NotNull(resultValue);
            
     
            var resultDict = resultValue as dynamic;
            Assert.NotNull(resultDict);
            
            _mockVocabularyRepository.Verify(r => r.GetByIdAsync(vocabularyId), Times.Once);
        }

        [Fact]
        public async Task GetById_WithVocabularyHavingNullOptionalFields_ShouldReturn200Ok()
        {
          
            var vocabularyId = 1;
            var vocabulary = new Vocabulary
            {
                VocabularyId = vocabularyId,
                VocabularyListId = 5,
                Word = "Hello",
                TypeOfWord = "noun",
                Category = null,
                Definition = "Xin chào",
                Example = null
            };

            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync(vocabulary);

         
            var result = await _controller.GetById(vocabularyId);

            
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.GetByIdAsync(vocabularyId), Times.Once);
        }

        #endregion

        #region Vocabulary Not Found Tests

        [Fact]
        public async Task GetById_WithNonExistentVocabularyId_ShouldReturn404NotFound()
        {
         
            var vocabularyId = 999;

            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync((Vocabulary?)null);

        
            var result = await _controller.GetById(vocabularyId);

         
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.GetByIdAsync(vocabularyId), Times.Once);
        }

        #endregion

        #region Boundary Cases Tests

        [Fact]
        public async Task GetById_WithZeroVocabularyId_ShouldReturn404NotFound()
        {
           
            var vocabularyId = 0;

            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync((Vocabulary?)null);

            
            var result = await _controller.GetById(vocabularyId);

           
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task GetById_WithNegativeVocabularyId_ShouldReturn404NotFound()
        {
           
            var vocabularyId = -1;

            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync((Vocabulary?)null);

           
            var result = await _controller.GetById(vocabularyId);

          
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task GetById_WhenRepositoryThrowsException_ShouldThrowException()
        {
           
            var vocabularyId = 1;

            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ThrowsAsync(new Exception("Database error"));

        
            await Assert.ThrowsAsync<Exception>(async () => await _controller.GetById(vocabularyId));
            _mockVocabularyRepository.Verify(r => r.GetByIdAsync(vocabularyId), Times.Once);
        }

        #endregion
    }
}

