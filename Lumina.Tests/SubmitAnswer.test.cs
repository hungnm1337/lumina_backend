using DataLayer.DTOs.UserAnswer;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Exam.Reading;

namespace Lumina.Tests
{
    public class SubmitAnswerTests
    {
        private readonly Mock<IReadingService> _mockReadingService;
        private readonly ReadingController _controller;

        public SubmitAnswerTests()
        {
            _mockReadingService = new Mock<IReadingService>();
            _controller = new ReadingController(_mockReadingService.Object);
        }

        [Fact]
        public async Task SubmitAnswer_WithValidRequest_Returns200OKWithResponse()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 10,
                SelectedOptionId = 3
            };

            var expectedResponse = new SubmitAnswerResponseDTO
            {
                Success = true,
                IsCorrect = true,
                Score = 5,
                Message = "Answer submitted successfully"
            };

            _mockReadingService
                .Setup(s => s.SubmitAnswerAsync(It.IsAny<ReadingAnswerRequestDTO>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<SubmitAnswerResponseDTO>(okResult.Value);
            Assert.True(response.Success);
            Assert.True(response.IsCorrect);
            Assert.Equal(5, response.Score);
            _mockReadingService.Verify(s => s.SubmitAnswerAsync(It.IsAny<ReadingAnswerRequestDTO>()), Times.Once);
        }

        [Fact]
        public async Task SubmitAnswer_WithInvalidModelState_Returns400BadRequest()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 10,
                SelectedOptionId = 3
            };

            _controller.ModelState.AddModelError("QuestionId", "Question ID is required");

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var modelState = Assert.IsType<SerializableError>(badRequestResult.Value);
            Assert.True(modelState.ContainsKey("QuestionId"));
            _mockReadingService.Verify(s => s.SubmitAnswerAsync(It.IsAny<ReadingAnswerRequestDTO>()), Times.Never);
        }

        [Fact]
        public async Task SubmitAnswer_WhenServiceReturnsFailure_Returns400BadRequest()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 10,
                SelectedOptionId = 3
            };

            var failureResponse = new SubmitAnswerResponseDTO
            {
                Success = false,
                IsCorrect = false,
                Score = 0,
                Message = "Invalid question or option"
            };

            _mockReadingService
                .Setup(s => s.SubmitAnswerAsync(It.IsAny<ReadingAnswerRequestDTO>()))
                .ReturnsAsync(failureResponse);

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            
            dynamic responseValue = badRequestResult.Value!;
            var messageProperty = responseValue.GetType().GetProperty("message");
            Assert.Equal("Invalid question or option", messageProperty!.GetValue(responseValue));
        }

        [Fact]
        public async Task SubmitAnswer_WhenKeyNotFoundExceptionThrown_Returns404NotFound()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 999,
                QuestionId = 10,
                SelectedOptionId = 3
            };

            _mockReadingService
                .Setup(s => s.SubmitAnswerAsync(It.IsAny<ReadingAnswerRequestDTO>()))
                .ThrowsAsync(new KeyNotFoundException("Exam attempt not found"));

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            
            dynamic responseValue = notFoundResult.Value!;
            var messageProperty = responseValue.GetType().GetProperty("message");
            Assert.Equal("Exam attempt not found", messageProperty!.GetValue(responseValue));
        }

        [Fact]
        public async Task SubmitAnswer_WhenGenericExceptionThrown_Returns500InternalServerError()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 10,
                SelectedOptionId = 3
            };

            _mockReadingService
                .Setup(s => s.SubmitAnswerAsync(It.IsAny<ReadingAnswerRequestDTO>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            
            dynamic responseValue = statusCodeResult.Value!;
            var messageProperty = responseValue.GetType().GetProperty("message");
            var detailProperty = responseValue.GetType().GetProperty("detail");
            Assert.Equal("An error occurred while submitting the answer", messageProperty!.GetValue(responseValue));
            Assert.Equal("Database connection failed", detailProperty!.GetValue(responseValue));
        }

        [Fact]
        public async Task SubmitAnswer_WithIncorrectAnswer_Returns200OKWithIncorrectResult()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 10,
                SelectedOptionId = 2
            };

            var incorrectResponse = new SubmitAnswerResponseDTO
            {
                Success = true,
                IsCorrect = false,
                Score = 0,
                Message = "Answer is incorrect"
            };

            _mockReadingService
                .Setup(s => s.SubmitAnswerAsync(It.IsAny<ReadingAnswerRequestDTO>()))
                .ReturnsAsync(incorrectResponse);

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var response = Assert.IsType<SubmitAnswerResponseDTO>(okResult.Value);
            Assert.True(response.Success);
            Assert.False(response.IsCorrect);
            Assert.Equal(0, response.Score);
        }
    }
}
