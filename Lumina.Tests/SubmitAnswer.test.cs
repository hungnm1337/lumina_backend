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
        public async Task SubmitAnswer_KhiTatCaCacTruongHopLe_TraVe200OKVoiKetQuaDung()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 1,
                SelectedOptionId = 1
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
        public async Task SubmitAnswer_KhiTatCaCacTruongBangKhong_TraVe400BadRequest()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 0,
                QuestionId = 0,
                SelectedOptionId = 0
            };

            var failureResponse = new SubmitAnswerResponseDTO
            {
                Success = false,
                IsCorrect = false,
                Score = 0,
                Message = "Invalid request data"
            };

            _mockReadingService
                .Setup(s => s.SubmitAnswerAsync(It.IsAny<ReadingAnswerRequestDTO>()))
                .ReturnsAsync(failureResponse);

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("Invalid request data", message);
        }

        [Fact]
        public async Task SubmitAnswer_KhiTatCaCacTruongBangAmMot_TraVe400BadRequest()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = -1,
                QuestionId = -1,
                SelectedOptionId = -1
            };

            var failureResponse = new SubmitAnswerResponseDTO
            {
                Success = false,
                IsCorrect = false,
                Score = 0,
                Message = "Invalid request data"
            };

            _mockReadingService
                .Setup(s => s.SubmitAnswerAsync(It.IsAny<ReadingAnswerRequestDTO>()))
                .ReturnsAsync(failureResponse);

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            
            var value = badRequestResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("Invalid request data", message);
        }

        [Fact]
        public async Task SubmitAnswer_KhiTatCaCacTruongKhongTonTai_TraVe404NotFound()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 999,
                QuestionId = 999,
                SelectedOptionId = 999
            };

            _mockReadingService
                .Setup(s => s.SubmitAnswerAsync(It.IsAny<ReadingAnswerRequestDTO>()))
                .ThrowsAsync(new KeyNotFoundException("Exam attempt, question, or option not found"));

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            
            var value = notFoundResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("Exam attempt, question, or option not found", message);
        }

        [Fact]
        public async Task SubmitAnswer_KhiServiceThrowException_TraVe500InternalServerError()
        {
            // Arrange
            var request = new ReadingAnswerRequestDTO
            {
                ExamAttemptId = 1,
                QuestionId = 1,
                SelectedOptionId = 1
            };

            _mockReadingService
                .Setup(s => s.SubmitAnswerAsync(It.IsAny<ReadingAnswerRequestDTO>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.SubmitAnswer(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            
            var value = statusCodeResult.Value;
            Assert.NotNull(value);
            var messageProperty = value.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            var message = messageProperty.GetValue(value)?.ToString();
            Assert.Equal("An error occurred while submitting the answer", message);
            
            var detailProperty = value.GetType().GetProperty("detail");
            Assert.NotNull(detailProperty);
            var detail = detailProperty.GetValue(value)?.ToString();
            Assert.Equal("Database connection failed", detail);
        }
    }
}
