using DataLayer.DTOs.UserAnswer;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Exam.Listening;

namespace Lumina.Tests
{
    public class ListeningControllerSubmitAnswerTests
    {
        private readonly Mock<IListeningService> _mockListeningService;
        private readonly ListeningController _controller;

        public ListeningControllerSubmitAnswerTests()
        {
            _mockListeningService = new Mock<IListeningService>();
            _controller = new ListeningController(_mockListeningService.Object);
        }

        #region Happy Path Tests

        [Fact]
        public async Task SubmitAnswer_WithValidRequest_ShouldReturn200OK()
        {
            // Arrange
            var request = new SubmitAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 10,
                SelectedOptionId = 25
            };

            var expectedResponse = new SubmitAnswerResponseDTO
            {
                Success = true,
                IsCorrect = true,
                Score = 5.0m,
                Message = "Answer submitted successfully"
            };

            _mockListeningService.Setup(s => s.SubmitAnswerAsync(It.IsAny<SubmitAnswerRequestDTO>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            
            var actualResponse = Assert.IsType<SubmitAnswerResponseDTO>(okResult.Value);
            Assert.True(actualResponse.Success);
            Assert.True(actualResponse.IsCorrect);
            Assert.Equal(5.0m, actualResponse.Score);
        }

        [Fact]
        public async Task SubmitAnswer_WithValidRequest_ShouldCallServiceOnce()
        {
            // Arrange
            var request = new SubmitAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 10,
                SelectedOptionId = 25
            };

            var expectedResponse = new SubmitAnswerResponseDTO
            {
                Success = true,
                IsCorrect = true,
                Score = 5.0m,
                Message = "Answer submitted successfully"
            };

            _mockListeningService.Setup(s => s.SubmitAnswerAsync(It.IsAny<SubmitAnswerRequestDTO>()))
                .ReturnsAsync(expectedResponse);

            // Act
            await _controller.SubmitAnswer(request);

            // Assert
            _mockListeningService.Verify(s => s.SubmitAnswerAsync(It.Is<SubmitAnswerRequestDTO>(
                r => r.ExamAttemptId == request.ExamAttemptId &&
                     r.QuestionId == request.QuestionId &&
                     r.SelectedOptionId == request.SelectedOptionId
            )), Times.Once);
        }

        #endregion

        #region Invalid ModelState Tests

        [Fact]
        public async Task SubmitAnswer_WithInvalidModelState_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new SubmitAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 10,
                SelectedOptionId = 25
            };

            _controller.ModelState.AddModelError("ExamAttemptId", "ExamAttemptId is required");

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task SubmitAnswer_WithInvalidModelState_ShouldNotCallService()
        {
            // Arrange
            var request = new SubmitAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 10,
                SelectedOptionId = 25
            };

            _controller.ModelState.AddModelError("QuestionId", "QuestionId is required");

            // Act
            await _controller.SubmitAnswer(request);

            // Assert
            _mockListeningService.Verify(s => s.SubmitAnswerAsync(It.IsAny<SubmitAnswerRequestDTO>()), Times.Never);
        }

        #endregion

        #region Service Returns Success False Tests

        [Fact]
        public async Task SubmitAnswer_WhenServiceReturnsSuccessFalse_ShouldReturn400BadRequest()
        {
            // Arrange
            var request = new SubmitAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 10,
                SelectedOptionId = 25
            };

            var failedResponse = new SubmitAnswerResponseDTO
            {
                Success = false,
                IsCorrect = false,
                Score = 0m,
                Message = "Invalid exam attempt or question not found"
            };

            _mockListeningService.Setup(s => s.SubmitAnswerAsync(It.IsAny<SubmitAnswerRequestDTO>()))
                .ReturnsAsync(failedResponse);

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            
            var actualResponse = Assert.IsType<SubmitAnswerResponseDTO>(badRequestResult.Value);
            Assert.False(actualResponse.Success);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task SubmitAnswer_WhenServiceThrowsException_ShouldReturn500InternalServerError()
        {
            // Arrange
            var request = new SubmitAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 10,
                SelectedOptionId = 25
            };

            _mockListeningService.Setup(s => s.SubmitAnswerAsync(It.IsAny<SubmitAnswerRequestDTO>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task SubmitAnswer_WhenServiceThrowsException_ShouldReturnErrorMessage()
        {
            // Arrange
            var request = new SubmitAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 10,
                SelectedOptionId = 25
            };

            var exceptionMessage = "Database connection failed";
            _mockListeningService.Setup(s => s.SubmitAnswerAsync(It.IsAny<SubmitAnswerRequestDTO>()))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            var responseValue = statusCodeResult.Value;
            
            var messageProperty = responseValue.GetType().GetProperty("message");
            var errorProperty = responseValue.GetType().GetProperty("error");
            
            Assert.NotNull(messageProperty);
            Assert.NotNull(errorProperty);
            Assert.Equal("An error occurred while submitting the answer.", messageProperty.GetValue(responseValue));
            Assert.Equal(exceptionMessage, errorProperty.GetValue(responseValue));
        }

        #endregion
    }
}
