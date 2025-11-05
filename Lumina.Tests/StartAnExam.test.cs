using DataLayer.DTOs.UserAnswer;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Exam.ExamAttempt;

namespace Lumina.Tests
{
    public class StartAnExamTests
    {
        private readonly Mock<IExamAttemptService> _mockExamAttemptService;
        private readonly Mock<ILogger<ExamAttemptController>> _mockLogger;
        private readonly ExamAttemptController _controller;

        public StartAnExamTests()
        {
            _mockExamAttemptService = new Mock<IExamAttemptService>();
            _mockLogger = new Mock<ILogger<ExamAttemptController>>();
            _controller = new ExamAttemptController(_mockExamAttemptService.Object, _mockLogger.Object);
        }

        #region Valid Input Tests

        [Fact]
        public async Task StartAnExam_WithValidInput_ShouldReturn200OK()
        {
            // Arrange
            var request = new ExamAttemptRequestDTO
            {
                UserID = 1,
                ExamID = 1,
                StartTime = DateTime.UtcNow
            };

            var expectedResponse = new ExamAttemptRequestDTO
            {
                AttemptID = 1,
                UserID = 1,
                ExamID = 1,
                StartTime = request.StartTime,
                Status = "InProgress"
            };

            _mockExamAttemptService.Setup(s => s.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.StartAnExam(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<ExamAttemptRequestDTO>(okResult.Value);
            Assert.Equal(1, response.AttemptID);
            Assert.Equal("InProgress", response.Status);
        }

        #endregion

        #region Null Request Tests

        [Fact]
        public async Task StartAnExam_WithNullRequest_ShouldReturn400BadRequest()
        {
            // Arrange
            ExamAttemptRequestDTO request = null!;

            // Act
            var result = await _controller.StartAnExam(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("Request body cannot be null.", messageProperty!.GetValue(response));
        }

        #endregion

        #region Invalid UserID Tests

        [Fact]
        public async Task StartAnExam_WithZeroUserID_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new ExamAttemptRequestDTO
            {
                UserID = 0,
                ExamID = 1
            };

            // Act
            var result = await _controller.StartAnExam(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("Invalid UserID.", messageProperty!.GetValue(response));
        }

        [Fact]
        public async Task StartAnExam_WithNegativeUserID_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new ExamAttemptRequestDTO
            {
                UserID = -1,
                ExamID = 1
            };

            // Act
            var result = await _controller.StartAnExam(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        #endregion

        #region Invalid ExamID Tests

        [Fact]
        public async Task StartAnExam_WithZeroExamID_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new ExamAttemptRequestDTO
            {
                UserID = 1,
                ExamID = 0
            };

            // Act
            var result = await _controller.StartAnExam(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("Invalid ExamID.", messageProperty!.GetValue(response));
        }

        [Fact]
        public async Task StartAnExam_WithNegativeExamID_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new ExamAttemptRequestDTO
            {
                UserID = 1,
                ExamID = -1
            };

            // Act
            var result = await _controller.StartAnExam(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        #endregion

        #region Validation Order Tests

        [Fact]
        public async Task StartAnExam_WithBothInvalid_ShouldCheckUserIDFirst()
        {
            // Arrange - Both invalid, but UserID checked first
            var request = new ExamAttemptRequestDTO
            {
                UserID = 0,
                ExamID = 0
            };

            // Act
            var result = await _controller.StartAnExam(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("Invalid UserID.", messageProperty!.GetValue(response));
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task StartAnExam_WhenServiceThrowsException_ShouldReturn500()
        {
            // Arrange
            var request = new ExamAttemptRequestDTO
            {
                UserID = 1,
                ExamID = 1
            };

            _mockExamAttemptService.Setup(s => s.StartAnExam(It.IsAny<ExamAttemptRequestDTO>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.StartAnExam(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal("An unexpected error occurred while starting the exam.", statusCodeResult.Value);
        }

        #endregion
    }
}
