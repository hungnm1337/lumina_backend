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
    public class GetStudentListVocabularyTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITextToSpeechService> _mockTtsService;
        private readonly Mock<IVocabularyRepository> _mockVocabularyRepository;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly VocabulariesController _controller;

        public GetStudentListVocabularyTests()
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
        public async Task GetStudentList_WithListIdAndSearch_ShouldReturn200Ok()
        {
           
            var listId = 1;
            var search = "Hello";
            var userId = 1;
            var vocabularyList = new VocabularyList { VocabularyListId = listId, Name = "Test List" };
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

            SetupUserClaims(userId);
            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(listId)).ReturnsAsync(vocabularyList);
            _mockVocabularyRepository.Setup(r => r.GetByListAsync(listId, search)).ReturnsAsync(vocabularies);

            var result = await _controller.GetStudentList(listId, search);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyListRepository.Verify(r => r.FindByIdAsync(listId), Times.Once);
            _mockVocabularyRepository.Verify(r => r.GetByListAsync(listId, search), Times.Once);
        }

        [Fact]
        public async Task GetStudentList_WithoutListIdAndSearch_ShouldReturn200Ok()
        {
        
            int? listId = null;
            string? search = null;
            var userId = 1;
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

            SetupUserClaims(userId);
            _mockVocabularyRepository.Setup(r => r.GetByListAsync(listId, search)).ReturnsAsync(vocabularies);


            var result = await _controller.GetStudentList(listId, search);

       
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyListRepository.Verify(r => r.FindByIdAsync(It.IsAny<int>()), Times.Never);
            _mockVocabularyRepository.Verify(r => r.GetByListAsync(listId, search), Times.Once);
        }

        [Fact]
        public async Task GetStudentList_WithEmptyList_ShouldReturn200Ok()
        {
      
            int? listId = 1;
            string? search = null;
            var userId = 1;
            var vocabularyList = new VocabularyList { VocabularyListId = 1, Name = "Test List" };
            var vocabularies = new List<Vocabulary>();

            SetupUserClaims(userId);
            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(vocabularyList);
            _mockVocabularyRepository.Setup(r => r.GetByListAsync(listId, search)).ReturnsAsync(vocabularies);

         
            var result = await _controller.GetStudentList(listId, search);

           
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var resultValue = okResult.Value as IEnumerable<object>;
            Assert.NotNull(resultValue);
            Assert.Empty(resultValue);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task GetStudentList_WithNullUserIdClaim_ShouldReturn401Unauthorized()
        {
       
            int? listId = null;
            string? search = null;
            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

         
            var result = await _controller.GetStudentList(listId, search);

         
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid token - User ID could not be determined.", errorResponse.Error);
        }

        [Fact]
        public async Task GetStudentList_WithInvalidUserIdClaim_ShouldReturn401Unauthorized()
        {
           
            int? listId = null;
            string? search = null;
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

           
            var result = await _controller.GetStudentList(listId, search);

         
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid token - User ID could not be determined.", errorResponse.Error);
        }

        #endregion

        #region VocabularyList Not Found Tests

        [Fact]
        public async Task GetStudentList_WithNonExistentListId_ShouldReturn404NotFound()
        {
         
            int? listId = 999;
            string? search = null;
            var userId = 1;

            SetupUserClaims(userId);
            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((VocabularyList?)null);

      
            var result = await _controller.GetStudentList(listId, search);

       
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Equal("Vocabulary list not found.", errorResponse.Error);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task GetStudentList_WhenRepositoryThrowsException_ShouldReturn500InternalServerError()
        {
         
            int? listId = 1;
            string? search = null;
            var userId = 1;
            var vocabularyList = new VocabularyList { VocabularyListId = 1, Name = "Test List" };

            SetupUserClaims(userId);
            _mockVocabularyListRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(vocabularyList);
            _mockVocabularyRepository.Setup(r => r.GetByListAsync(listId, search)).ThrowsAsync(new Exception("Database error"));

          
            var result = await _controller.GetStudentList(listId, search);

        
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

