using DataLayer.DTOs.UserAnswer;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Exam.ExamAttempt;

namespace Lumina.Tests
{
    public class EndAnExamTests
    {
        private readonly Mock<IExamAttemptService> _mockExamAttemptService;
        private readonly Mock<ILogger<ExamAttemptController>> _mockLogger;
        private readonly ExamAttemptController _controller;

        public EndAnExamTests()
        {
            _mockExamAttemptService = new Mock<IExamAttemptService>();
            _mockLogger = new Mock<ILogger<ExamAttemptController>>();
            _controller = new ExamAttemptController(_mockExamAttemptService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task EndAnExam_ValidAttemptID_Returns200OK()
        {
            // Arrange
            var request = new ExamAttemptRequestDTO
            {
                AttemptID = 1,
                EndTime = DateTime.UtcNow
            };

            var expectedResponse = new ExamAttemptRequestDTO
            {
                AttemptID = 1,
                Status = "Completed",
                EndTime = request.EndTime,
                Score = 850
            };

            _mockExamAttemptService.Setup(s => s.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.EndAnExam(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<ExamAttemptRequestDTO>(okResult.Value);
            Assert.Equal("Completed", response.Status);
            Assert.Equal(850, response.Score);
        }

        [Fact]
        public async Task EndAnExam_NullRequest_Returns400BadRequest()
        {
            // Arrange
            ExamAttemptRequestDTO request = null!;

            // Act
            var result = await _controller.EndAnExam(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("Request body cannot be null.", messageProperty!.GetValue(response));
        }

        [Fact]
        public async Task EndAnExam_ZeroAttemptID_Returns400BadRequest()
        {
            // Arrange
            var request = new ExamAttemptRequestDTO
            {
                AttemptID = 0
            };

            // Act
            var result = await _controller.EndAnExam(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("Invalid AttemptID.", messageProperty!.GetValue(response));
        }

        [Fact]
        public async Task EndAnExam_NegativeAttemptID_Returns400BadRequest()
        {
            // Arrange
            var request = new ExamAttemptRequestDTO
            {
                AttemptID = -1
            };

            // Act
            var result = await _controller.EndAnExam(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task EndAnExam_NonExistentAttemptID_Returns404NotFound()
        {
            // Arrange
            var request = new ExamAttemptRequestDTO
            {
                AttemptID = 999
            };

            _mockExamAttemptService.Setup(s => s.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()))
                .ThrowsAsync(new KeyNotFoundException("Exam attempt not found."));

            // Act
            var result = await _controller.EndAnExam(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            var response = notFoundResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("Exam attempt not found.", messageProperty!.GetValue(response));
        }

        [Fact]
        public async Task EndAnExam_ServiceThrowsKeyNotFoundException_LogsWarning()
        {
            // Arrange
            var request = new ExamAttemptRequestDTO
            {
                AttemptID = 999
            };

            _mockExamAttemptService.Setup(s => s.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()))
                .ThrowsAsync(new KeyNotFoundException("Exam attempt not found."));

            // Act
            await _controller.EndAnExam(request);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception?>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task EndAnExam_ServiceThrowsGenericException_Returns500InternalServerError()
        {
            // Arrange
            var request = new ExamAttemptRequestDTO
            {
                AttemptID = 1
            };

            _mockExamAttemptService.Setup(s => s.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.EndAnExam(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal("An unexpected error occurred while ending the exam.", statusCodeResult.Value);
        }

        [Fact]
        public async Task EndAnExam_ServiceThrowsGenericException_LogsError()
        {
            // Arrange
            var request = new ExamAttemptRequestDTO
            {
                AttemptID = 1
            };

            _mockExamAttemptService.Setup(s => s.EndAnExam(It.IsAny<ExamAttemptRequestDTO>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            await _controller.EndAnExam(request);

            // Assert
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
