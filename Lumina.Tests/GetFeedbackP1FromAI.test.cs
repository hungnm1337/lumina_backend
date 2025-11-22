using DataLayer.DTOs.Exam.Writting;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Exam.Writting;

namespace Lumina.Tests
{
    public class GetFeedbackP1FromAITests
    {
        private readonly Mock<IWritingService> _mockWritingService;
        private readonly Mock<ILogger<WritingController>> _mockLogger;
        private readonly WritingController _controller;

        public GetFeedbackP1FromAITests()
        {
            _mockWritingService = new Mock<IWritingService>();
            _mockLogger = new Mock<ILogger<WritingController>>();
            _controller = new WritingController(_mockWritingService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetFeedbackP1FromAI_WithValidInput_Returns200OKWithResponse()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "A woman is reading a book in the library",
                VocabularyRequest = "library, book, reading",
                UserAnswer = "The woman is reading a book in the library."
            };

            var expectedResponse = new WritingResponseDTO
            {
                TotalScore = 90,
                GrammarFeedback = "Excellent grammar",
                VocabularyFeedback = "Good use of vocabulary",
                ContentAccuracyFeedback = "Accurate description",
                CorreededAnswerProposal = "The woman is reading a book in the library."
            };

            _mockWritingService.Setup(s => s.GetFeedbackP1FromAI(It.IsAny<WritingRequestP1DTO>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetFeedbackP1FromAI(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<WritingResponseDTO>(okResult.Value);
            Assert.Equal(90, response.TotalScore);
            Assert.Equal("Excellent grammar", response.GrammarFeedback);
            _mockWritingService.Verify(s => s.GetFeedbackP1FromAI(It.IsAny<WritingRequestP1DTO>()), Times.Once);
        }

        [Fact]
        public async Task GetFeedbackP1FromAI_WithInvalidModelState_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "A woman is reading a book in the library",
                UserAnswer = "The woman is reading a book in the library."
            };

            _controller.ModelState.AddModelError("PictureCaption", "Picture caption is required");

            // Act
            var result = await _controller.GetFeedbackP1FromAI(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var modelState = Assert.IsType<SerializableError>(badRequestResult.Value);
            Assert.True(modelState.ContainsKey("PictureCaption"));
            _mockWritingService.Verify(s => s.GetFeedbackP1FromAI(It.IsAny<WritingRequestP1DTO>()), Times.Never);
        }

        [Fact]
        public async Task GetFeedbackP1FromAI_WithNullUserAnswer_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "A woman is reading a book",
                UserAnswer = null!
            };

            // Act
            var result = await _controller.GetFeedbackP1FromAI(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("UserAnswer cannot be empty.", messageProperty!.GetValue(response));
        }

        [Fact]
        public async Task GetFeedbackP1FromAI_WithEmptyUserAnswer_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "A woman is reading a book",
                UserAnswer = ""
            };

            // Act
            var result = await _controller.GetFeedbackP1FromAI(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task GetFeedbackP1FromAI_WithWhitespaceUserAnswer_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "A woman is reading a book",
                UserAnswer = "   "
            };

            // Act
            var result = await _controller.GetFeedbackP1FromAI(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            _mockWritingService.Verify(s => s.GetFeedbackP1FromAI(It.IsAny<WritingRequestP1DTO>()), Times.Never);
        }

        [Fact]
        public async Task GetFeedbackP1FromAI_WithNullPictureCaption_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = null!,
                UserAnswer = "This is my answer"
            };

            // Act
            var result = await _controller.GetFeedbackP1FromAI(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("PictureCaption cannot be empty.", messageProperty!.GetValue(response));
        }

        [Fact]
        public async Task GetFeedbackP1FromAI_WithEmptyPictureCaption_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "",
                UserAnswer = "This is my answer"
            };

            // Act
            var result = await _controller.GetFeedbackP1FromAI(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task GetFeedbackP1FromAI_WithWhitespacePictureCaption_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "   ",
                UserAnswer = "This is my answer"
            };

            // Act
            var result = await _controller.GetFeedbackP1FromAI(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            _mockWritingService.Verify(s => s.GetFeedbackP1FromAI(It.IsAny<WritingRequestP1DTO>()), Times.Never);
        }

        [Fact]
        public async Task GetFeedbackP1FromAI_WhenServiceThrowsException_Returns500AndLogsError()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "A woman is reading",
                VocabularyRequest = "library, book, reading",
                UserAnswer = "This is my answer"
            };

            _mockWritingService.Setup(s => s.GetFeedbackP1FromAI(It.IsAny<WritingRequestP1DTO>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetFeedbackP1FromAI(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var response = statusCodeResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("An unexpected error occurred while getting AI feedback.", messageProperty!.GetValue(response));
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception?>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }
    }
}
