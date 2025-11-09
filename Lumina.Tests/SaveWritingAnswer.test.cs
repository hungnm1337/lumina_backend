using DataLayer.DTOs.UserAnswer;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Exam.Writting;

namespace Lumina.Tests
{
    public class SaveWritingAnswerTests
    {
        private readonly Mock<IWritingService> _mockWritingService;
        private readonly Mock<ILogger<WritingController>> _mockLogger;
        private readonly WritingController _controller;

        public SaveWritingAnswerTests()
        {
            _mockWritingService = new Mock<IWritingService>();
            _mockLogger = new Mock<ILogger<WritingController>>();
            _controller = new WritingController(_mockWritingService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task SaveWritingAnswer_WithValidInput_Returns200OKWithSuccessMessage()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 1,
                QuestionId = 1,
                UserAnswerContent = "This is a sample answer"
            };
            _mockWritingService.Setup(s => s.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.SaveWritingAnswer(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = okResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("Writing answer saved successfully.", messageProperty!.GetValue(response));
            _mockWritingService.Verify(s => s.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()), Times.Once);
        }

        [Fact]
        public async Task SaveWritingAnswer_WithNullRequest_Returns400BadRequest()
        {
            // Arrange
            WritingAnswerRequestDTO request = null!;

            // Act
            var result = await _controller.SaveWritingAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("Request cannot be null.", messageProperty!.GetValue(response));
            _mockWritingService.Verify(s => s.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()), Times.Never);
        }

        [Fact]
        public async Task SaveWritingAnswer_WithZeroAttemptID_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 0,
                QuestionId = 1,
                UserAnswerContent = "This is a sample answer"
            };

            // Act
            var result = await _controller.SaveWritingAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("Invalid AttemptID.", messageProperty!.GetValue(response));
        }

        [Fact]
        public async Task SaveWritingAnswer_WithNegativeAttemptID_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = -1,
                QuestionId = 1,
                UserAnswerContent = "This is a sample answer"
            };

            // Act
            var result = await _controller.SaveWritingAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task SaveWritingAnswer_WithZeroQuestionId_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 1,
                QuestionId = 0,
                UserAnswerContent = "This is a sample answer"
            };

            // Act
            var result = await _controller.SaveWritingAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("Invalid QuestionId.", messageProperty!.GetValue(response));
        }

        [Fact]
        public async Task SaveWritingAnswer_WithNegativeQuestionId_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 1,
                QuestionId = -1,
                UserAnswerContent = "This is a sample answer"
            };

            // Act
            var result = await _controller.SaveWritingAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task SaveWritingAnswer_WithEmptyUserAnswerContent_Returns400BadRequest()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 1,
                QuestionId = 1,
                UserAnswerContent = ""
            };

            // Act
            var result = await _controller.SaveWritingAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("UserAnswerContent cannot be empty.", messageProperty!.GetValue(response));
        }

        [Fact]
        public async Task SaveWritingAnswer_WhenServiceReturnsFalse_Returns500WithFailureMessage()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 1,
                QuestionId = 1,
                UserAnswerContent = "This is a sample answer"
            };
            _mockWritingService.Setup(s => s.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.SaveWritingAnswer(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var response = statusCodeResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("Failed to save writing answer.", messageProperty!.GetValue(response));
        }

        [Fact]
        public async Task SaveWritingAnswer_WhenServiceThrowsException_Returns500AndLogsError()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 1,
                QuestionId = 1,
                UserAnswerContent = "This is a sample answer"
            };
            _mockWritingService.Setup(s => s.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.SaveWritingAnswer(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var response = statusCodeResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("An unexpected error occurred while saving writing answer.", messageProperty!.GetValue(response));

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
