using DataLayer.DTOs.Exam.Writting;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Exam.Writting;

namespace Lumina.Tests
{
    public class GetFeedbackP23FromAITests
    {
        private readonly Mock<IWritingService> _mockWritingService;
        private readonly Mock<ILogger<WritingController>> _mockLogger;
        private readonly WritingController _controller;

        public GetFeedbackP23FromAITests()
        {
            _mockWritingService = new Mock<IWritingService>();
            _mockLogger = new Mock<ILogger<WritingController>>();
            _controller = new WritingController(_mockWritingService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetFeedbackP23FromAI_WithValidInput_Returns200OKWithResponse()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "Write an email to your friend",
                UserAnswer = "Dear John, How are you? I hope you are doing well."
            };

            var expectedResponse = new WritingResponseDTO
            {
                TotalScore = 85,
                GrammarFeedback = "Good grammar usage",
                VocabularyFeedback = "Good vocabulary",
                ContentAccuracyFeedback = "Content is accurate",
                CorreededAnswerProposal = "Dear John, How are you? I hope you are doing well."
            };

            _mockWritingService.Setup(s => s.GetFeedbackP23FromAI(It.IsAny<WritingRequestP23DTO>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetFeedbackP23FromAI(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<WritingResponseDTO>(okResult.Value);
            Assert.Equal(85, response.TotalScore);
            Assert.Equal("Good grammar usage", response.GrammarFeedback);
            _mockWritingService.Verify(s => s.GetFeedbackP23FromAI(It.IsAny<WritingRequestP23DTO>()), Times.Once);
        }

        [Fact]
        public async Task GetFeedbackP23FromAI_WithInvalidModelState_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "Write an email",
                UserAnswer = "This is my answer"
            };

            _controller.ModelState.AddModelError("PartNumber", "Part number is required");

            // Act
            var result = await _controller.GetFeedbackP23FromAI(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var modelState = Assert.IsType<SerializableError>(badRequestResult.Value);
            Assert.True(modelState.ContainsKey("PartNumber"));
            _mockWritingService.Verify(s => s.GetFeedbackP23FromAI(It.IsAny<WritingRequestP23DTO>()), Times.Never);
        }

        [Fact]
        public async Task GetFeedbackP23FromAI_WithNullUserAnswer_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "Write an email",
                UserAnswer = null!
            };

            // Act
            var result = await _controller.GetFeedbackP23FromAI(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("UserAnswer cannot be empty.", messageProperty!.GetValue(response));
        }

        [Fact]
        public async Task GetFeedbackP23FromAI_WithEmptyUserAnswer_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "Write an email",
                UserAnswer = ""
            };

            // Act
            var result = await _controller.GetFeedbackP23FromAI(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task GetFeedbackP23FromAI_WithWhitespaceUserAnswer_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "Write an email",
                UserAnswer = "   "
            };

            // Act
            var result = await _controller.GetFeedbackP23FromAI(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            _mockWritingService.Verify(s => s.GetFeedbackP23FromAI(It.IsAny<WritingRequestP23DTO>()), Times.Never);
        }

        [Fact]
        public async Task GetFeedbackP23FromAI_WithNullPrompt_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = null!,
                UserAnswer = "This is my answer"
            };

            // Act
            var result = await _controller.GetFeedbackP23FromAI(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("Prompt cannot be empty.", messageProperty!.GetValue(response));
        }

        [Fact]
        public async Task GetFeedbackP23FromAI_WithEmptyPrompt_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "",
                UserAnswer = "This is my answer"
            };

            // Act
            var result = await _controller.GetFeedbackP23FromAI(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task GetFeedbackP23FromAI_WithWhitespacePrompt_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "   ",
                UserAnswer = "This is my answer"
            };

            // Act
            var result = await _controller.GetFeedbackP23FromAI(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            _mockWritingService.Verify(s => s.GetFeedbackP23FromAI(It.IsAny<WritingRequestP23DTO>()), Times.Never);
        }

        [Fact]
        public async Task GetFeedbackP23FromAI_WhenServiceThrowsException_Returns500AndLogsError()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "Write an email",
                UserAnswer = "This is my answer"
            };

            _mockWritingService.Setup(s => s.GetFeedbackP23FromAI(It.IsAny<WritingRequestP23DTO>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetFeedbackP23FromAI(request);

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
