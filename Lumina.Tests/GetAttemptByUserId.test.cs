using DataLayer.DTOs.UserAnswer;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Exam.ExamAttempt;

namespace Lumina.Tests
{
    public class GetAttemptByUserIdTests
    {
        private readonly Mock<IExamAttemptService> _mockExamAttemptService;
        private readonly Mock<ILogger<ExamAttemptController>> _mockLogger;
        private readonly ExamAttemptController _controller;

        public GetAttemptByUserIdTests()
        {
            _mockExamAttemptService = new Mock<IExamAttemptService>();
            _mockLogger = new Mock<ILogger<ExamAttemptController>>();
            _controller = new ExamAttemptController(_mockExamAttemptService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAttemptByUserId_ValidUserId_Returns200OKWithAttempts()
        {
            // Arrange
            int userId = 1;
            var expectedAttempts = new List<ExamAttemptResponseDTO>
            {
                new ExamAttemptResponseDTO 
                { 
                    AttemptID = 1, 
                    UserName = "John Doe", 
                    ExamName = "TOEIC Test 1", 
                    Score = 850,
                    Status = "Completed" 
                },
                new ExamAttemptResponseDTO 
                { 
                    AttemptID = 2, 
                    UserName = "John Doe", 
                    ExamName = "TOEIC Test 2", 
                    Score = 900,
                    Status = "Completed" 
                }
            };

            _mockExamAttemptService.Setup(s => s.GetAllExamAttempts(userId))
                .ReturnsAsync(expectedAttempts);

            // Act
            var result = await _controller.GetAttemptByUserId(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var attempts = Assert.IsAssignableFrom<IEnumerable<ExamAttemptResponseDTO>>(okResult.Value);
            Assert.Equal(2, attempts.Count());
            Assert.Equal(850, attempts.First().Score);
        }

        [Fact]
        public async Task GetAttemptByUserId_ValidUserIdWithEmptyList_Returns200OKWithEmptyList()
        {
            // Arrange
            int userId = 999;
            var emptyAttempts = new List<ExamAttemptResponseDTO>();

            _mockExamAttemptService.Setup(s => s.GetAllExamAttempts(userId))
                .ReturnsAsync(emptyAttempts);

            // Act
            var result = await _controller.GetAttemptByUserId(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var attempts = Assert.IsAssignableFrom<IEnumerable<ExamAttemptResponseDTO>>(okResult.Value);
            Assert.Empty(attempts);
        }

        [Fact]
        public async Task GetAttemptByUserId_ZeroUserId_Returns400BadRequest()
        {
            // Arrange
            int userId = 0;

            // Act
            var result = await _controller.GetAttemptByUserId(userId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("Invalid userId.", messageProperty!.GetValue(response));
        }

        [Fact]
        public async Task GetAttemptByUserId_NegativeUserId_Returns400BadRequest()
        {
            // Arrange
            int userId = -1;

            // Act
            var result = await _controller.GetAttemptByUserId(userId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var response = badRequestResult.Value;
            var messageProperty = response!.GetType().GetProperty("Message");
            Assert.Equal("Invalid userId.", messageProperty!.GetValue(response));
        }

        [Fact]
        public async Task GetAttemptByUserId_ServiceThrowsException_Returns500InternalServerError()
        {
            // Arrange
            int userId = 1;
            _mockExamAttemptService.Setup(s => s.GetAllExamAttempts(userId))
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            var result = await _controller.GetAttemptByUserId(userId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal("An unexpected error occurred while retrieving exam attempts.", statusCodeResult.Value);
        }
    }
}
