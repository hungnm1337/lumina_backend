using DataLayer.DTOs.UserAnswer;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Exam.ExamAttempt;

namespace Lumina.Tests
{
    public class GetExamAttemptByIdTests
    {
        private readonly Mock<IExamAttemptService> _mockExamAttemptService;
        private readonly Mock<ILogger<ExamAttemptController>> _mockLogger;
        private readonly ExamAttemptController _controller;

        public GetExamAttemptByIdTests()
        {
            _mockExamAttemptService = new Mock<IExamAttemptService>();
            _mockLogger = new Mock<ILogger<ExamAttemptController>>();
            _controller = new ExamAttemptController(_mockExamAttemptService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetExamAttemptById_ValidAttemptId_Returns200OKWithDetails()
        {
            // Arrange
            int attemptId = 1;
            var expectedDetails = new ExamAttemptDetailResponseDTO
            {
                ExamAttemptInfo = new ExamAttemptResponseDTO
                {
                    AttemptID = attemptId,
                    UserName = "John Doe",
                    ExamName = "TOEIC Test 1",
                    Score = 850,
                    Status = "Completed"
                },
                ListeningAnswers = new List<ListeningAnswerResponseDTO>(),
                ReadingAnswers = new List<ReadingAnswerResponseDTO>(),
                SpeakingAnswers = new List<SpeakingAnswerResponseDTO>(),
                WritingAnswers = new List<WritingAnswerResponseDTO>()
            };

            _mockExamAttemptService.Setup(s => s.GetExamAttemptById(attemptId))
                .ReturnsAsync(expectedDetails);

            // Act
            var result = await _controller.GetExamAttemptById(attemptId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var details = Assert.IsType<ExamAttemptDetailResponseDTO>(okResult.Value);
            Assert.Equal(attemptId, details.ExamAttemptInfo!.AttemptID);
            Assert.Equal("John Doe", details.ExamAttemptInfo.UserName);
        }

        [Fact]
        public async Task GetExamAttemptById_ZeroAttemptId_Returns400BadRequest()
        {
            // Arrange
            int attemptId = 0;

            // Act
            var result = await _controller.GetExamAttemptById(attemptId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("Invalid attemptId.", messageProperty!.GetValue(response));
        }

        [Fact]
        public async Task GetExamAttemptById_NegativeAttemptId_Returns400BadRequest()
        {
            // Arrange
            int attemptId = -1;

            // Act
            var result = await _controller.GetExamAttemptById(attemptId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task GetExamAttemptById_NonExistentAttemptId_Returns404NotFound()
        {
            // Arrange
            int attemptId = 999;
            _mockExamAttemptService.Setup(s => s.GetExamAttemptById(attemptId))
                .ReturnsAsync((ExamAttemptDetailResponseDTO)null!);

            // Act
            var result = await _controller.GetExamAttemptById(attemptId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            var response = notFoundResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal($"Exam attempt with ID {attemptId} not found.", messageProperty!.GetValue(response));
        }


        [Fact]
        public async Task GetExamAttemptById_ServiceThrowsException_Returns500InternalServerError()
        {
            // Arrange
            int attemptId = 1;
            _mockExamAttemptService.Setup(s => s.GetExamAttemptById(attemptId))
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            var result = await _controller.GetExamAttemptById(attemptId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal("An unexpected error occurred while retrieving exam attempt details.", statusCodeResult.Value);
        }
    }
}
