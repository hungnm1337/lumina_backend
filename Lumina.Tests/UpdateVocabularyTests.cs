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
    public class UpdateVocabularyTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITextToSpeechService> _mockTtsService;
        private readonly Mock<IVocabularyRepository> _mockVocabularyRepository;
        private readonly VocabulariesController _controller;

        public UpdateVocabularyTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTtsService = new Mock<ITextToSpeechService>();
            _mockVocabularyRepository = new Mock<IVocabularyRepository>();

            _mockUnitOfWork.Setup(u => u.Vocabularies).Returns(_mockVocabularyRepository.Object);

            _controller = new VocabulariesController(_mockUnitOfWork.Object, _mockTtsService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task Update_WithValidInput_ShouldReturn200Ok()
        {
          
            var vocabularyId = 1;
            var request = new VocabulariesController.UpdateVocabularyRequest
            {
                Word = "Hello",
                TypeOfWord = "noun",
                Category = "greeting",
                Definition = "Xin chào",
                Example = "Hello, how are you?"
            };
            var userId = 1;
            var existingVocabulary = new Vocabulary
            {
                VocabularyId = vocabularyId,
                Word = "Old Word",
                TypeOfWord = "verb",
                Definition = "Old Definition"
            };

            SetupUserClaims(userId);
            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync(existingVocabulary);
            _mockVocabularyRepository.Setup(r => r.UpdateAsync(It.IsAny<Vocabulary>()));
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

           
            var result = await _controller.Update(vocabularyId, request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.GetByIdAsync(vocabularyId), Times.Once);
            _mockVocabularyRepository.Verify(r => r.UpdateAsync(It.Is<Vocabulary>(v => 
                v.Word == request.Word &&
                v.TypeOfWord == request.TypeOfWord &&
                v.Category == request.Category &&
                v.Definition == request.Definition &&
                v.Example == request.Example
            )), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task Update_WithNullOptionalFields_ShouldReturn200Ok()
        {
            
            var vocabularyId = 1;
            var request = new VocabulariesController.UpdateVocabularyRequest
            {
                Word = "Hello",
                TypeOfWord = "noun",
                Category = null,
                Definition = "Xin chào",
                Example = null
            };
            var userId = 1;
            var existingVocabulary = new Vocabulary
            {
                VocabularyId = vocabularyId,
                Word = "Old Word",
                TypeOfWord = "verb",
                Definition = "Old Definition",
                Category = "old category",
                Example = "old example"
            };

            SetupUserClaims(userId);
            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync(existingVocabulary);
            _mockVocabularyRepository.Setup(r => r.UpdateAsync(It.IsAny<Vocabulary>()));
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

          
            var result = await _controller.Update(vocabularyId, request);

        
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            _mockVocabularyRepository.Verify(r => r.UpdateAsync(It.Is<Vocabulary>(v => 
                v.Category == null &&
                v.Example == null
            )), Times.Once);
        }

        #endregion

        #region ModelState Validation Tests

        [Fact]
        public async Task Update_WithInvalidModelState_ShouldReturn400BadRequest()
        {
            
            var vocabularyId = 1;
            var request = new VocabulariesController.UpdateVocabularyRequest
            {
                Word = "",
                TypeOfWord = "",
                Definition = ""
            };
            _controller.ModelState.AddModelError("Word", "Word is required");

   
            var result = await _controller.Update(vocabularyId, request);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task Update_WithNullUserIdClaim_ShouldReturn401Unauthorized()
        {
            
            var vocabularyId = 1;
            var request = new VocabulariesController.UpdateVocabularyRequest
            {
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

    
            var result = await _controller.Update(vocabularyId, request);

          
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid token - User ID could not be determined.", errorResponse.Error);
        }

        [Fact]
        public async Task Update_WithInvalidUserIdClaim_ShouldReturn401Unauthorized()
        {
           
            var vocabularyId = 1;
            var request = new VocabulariesController.UpdateVocabularyRequest
            {
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

       
            var result = await _controller.Update(vocabularyId, request);

      
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal("Invalid token - User ID could not be determined.", errorResponse.Error);
        }

        #endregion

        #region Vocabulary Not Found Tests

        [Fact]
        public async Task Update_WithNonExistentVocabularyId_ShouldReturn404NotFound()
        {
         
            var vocabularyId = 999;
            var request = new VocabulariesController.UpdateVocabularyRequest
            {
                Word = "Hello",
                TypeOfWord = "noun",
                Definition = "Xin chào"
            };
            var userId = 1;

            SetupUserClaims(userId);
            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync((Vocabulary?)null);

        
            var result = await _controller.Update(vocabularyId, request);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task Update_WhenRepositoryThrowsException_ShouldReturn500InternalServerError()
        {
           
            var vocabularyId = 1;
            var request = new VocabulariesController.UpdateVocabularyRequest
            {
                Word = "Hello",
                TypeOfWord = "noun",
                Definition = "Xin chào"
            };
            var userId = 1;
            var existingVocabulary = new Vocabulary
            {
                VocabularyId = vocabularyId,
                Word = "Old Word",
                TypeOfWord = "verb",
                Definition = "Old Definition"
            };

            SetupUserClaims(userId);
            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync(existingVocabulary);
            _mockVocabularyRepository.Setup(r => r.UpdateAsync(It.IsAny<Vocabulary>())).ThrowsAsync(new Exception("Database error"));

      
            var result = await _controller.Update(vocabularyId, request);

      
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("An internal server error occurred.", errorResponse.Error);
        }

        #endregion

        #region Boundary Cases Tests

        [Fact]
        public async Task Update_WithZeroVocabularyId_ShouldReturn404NotFound()
        {
            var vocabularyId = 0;
            var request = new VocabulariesController.UpdateVocabularyRequest
            {
                Word = "Hello",
                TypeOfWord = "noun",
                Definition = "Xin chào"
            };
            var userId = 1;

            SetupUserClaims(userId);
            _mockVocabularyRepository.Setup(r => r.GetByIdAsync(vocabularyId)).ReturnsAsync((Vocabulary?)null);

           
            var result = await _controller.Update(vocabularyId, request);

         
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

