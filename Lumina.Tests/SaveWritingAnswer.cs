
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

        #region Dependency Injection Tests

        [Fact]
        public void Constructor_WithValidDependencies_ShouldNotThrow()
        {
            // Arrange & Act & Assert
            var controller = new WritingController(_mockWritingService.Object, _mockLogger.Object);
            Assert.NotNull(controller);
        }

        #endregion

        #region Valid Input Tests

        [Fact]
        public async Task SaveWritingAnswer_WithValidInput_ShouldReturn200OK()
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
            var messageProperty = response.GetType().GetProperty("Message");
            Assert.NotNull(messageProperty);
            Assert.Equal("Writing answer saved successfully.", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task SaveWritingAnswer_WithValidInput_ShouldCallServiceOnce()
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
            await _controller.SaveWritingAnswer(request);

            // Assert
            _mockWritingService.Verify(s => s.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()), Times.Once);
        }

        [Fact]
        public async Task SaveWritingAnswer_WithValidInput_ShouldReturnSuccessMessage()
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
            var response = okResult.Value;
            var messageProperty = response.GetType().GetProperty("Message");
            Assert.Equal("Writing answer saved successfully.", messageProperty.GetValue(response));
        }

        #endregion

        #region Invalid AttemptID Tests

        [Fact]
        public async Task SaveWritingAnswer_WithZeroAttemptID_ShouldReturn400BadRequest()
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
        }

        [Fact]
        public async Task SaveWritingAnswer_WithNegativeAttemptID_ShouldReturn400BadRequest()
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
        public async Task SaveWritingAnswer_WithInvalidAttemptID_ShouldReturnErrorMessage()
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
            var response = badRequestResult.Value;
            var messageProperty = response.GetType().GetProperty("Message");
            Assert.Equal("Invalid AttemptID.", messageProperty.GetValue(response));
        }

        #endregion

        #region Invalid QuestionId Tests

        [Fact]
        public async Task SaveWritingAnswer_WithZeroQuestionId_ShouldReturn400BadRequest()
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
        }

        [Fact]
        public async Task SaveWritingAnswer_WithNegativeQuestionId_ShouldReturn400BadRequest()
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
        public async Task SaveWritingAnswer_WithInvalidQuestionId_ShouldReturnErrorMessage()
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
            var response = badRequestResult.Value;
            var messageProperty = response.GetType().GetProperty("Message");
            Assert.Equal("Invalid QuestionId.", messageProperty.GetValue(response));
        }

        #endregion

        #region Invalid UserAnswerContent Tests

        [Fact]
        public async Task SaveWritingAnswer_WithEmptyUserAnswerContent_ShouldReturn400BadRequest()
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
        }

        [Fact]
        public async Task SaveWritingAnswer_WithNullUserAnswerContent_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 1,
                QuestionId = 1,
                UserAnswerContent = null
            };

            // Act
            var result = await _controller.SaveWritingAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task SaveWritingAnswer_WithWhitespaceUserAnswerContent_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 1,
                QuestionId = 1,
                UserAnswerContent = "   "
            };

            // Act
            var result = await _controller.SaveWritingAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task SaveWritingAnswer_WithInvalidUserAnswerContent_ShouldReturnErrorMessage()
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
            var response = badRequestResult.Value;
            var messageProperty = response.GetType().GetProperty("Message");
            Assert.Equal("UserAnswerContent cannot be empty.", messageProperty.GetValue(response));
        }

        #endregion

        #region Service Return False Tests

        [Fact]
        public async Task SaveWritingAnswer_WhenServiceReturnsFalse_ShouldReturn500InternalServerError()
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
        }

        [Fact]
        public async Task SaveWritingAnswer_WhenServiceReturnsFalse_ShouldReturnFailureMessage()
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
            var response = statusCodeResult.Value;
            var messageProperty = response.GetType().GetProperty("Message");
            Assert.Equal("Failed to save writing answer.", messageProperty.GetValue(response));
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task SaveWritingAnswer_WhenServiceThrowsException_ShouldReturn500InternalServerError()
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
        }

        [Fact]
        public async Task SaveWritingAnswer_WhenServiceThrowsException_ShouldLogError()
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
            await _controller.SaveWritingAnswer(request);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task SaveWritingAnswer_WhenServiceThrowsException_ShouldReturnErrorMessage()
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
            var response = statusCodeResult.Value;
            var messageProperty = response.GetType().GetProperty("Message");
            Assert.Equal("An unexpected error occurred while saving writing answer.", messageProperty.GetValue(response));
        }

        #endregion

        #region Null Request Tests

        [Fact]
        public async Task SaveWritingAnswer_WithNullRequest_ShouldReturn400BadRequest()
        {
            // Arrange
            WritingAnswerRequestDTO request = null;

            // Act
            var result = await _controller.SaveWritingAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task SaveWritingAnswer_WithNullRequest_ShouldReturnErrorMessage()
        {
            // Arrange
            WritingAnswerRequestDTO request = null;

            // Act
            var result = await _controller.SaveWritingAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var messageProperty = response.GetType().GetProperty("Message");
            Assert.Equal("Request cannot be null.", messageProperty.GetValue(response));
        }

        [Fact]
        public async Task SaveWritingAnswer_WithNullRequest_ShouldNotCallService()
        {
            // Arrange
            WritingAnswerRequestDTO request = null;

            // Act
            await _controller.SaveWritingAnswer(request);

            // Assert
            _mockWritingService.Verify(s => s.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()), Times.Never);
        }

        #endregion
    }
}