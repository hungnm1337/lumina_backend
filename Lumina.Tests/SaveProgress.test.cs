// File: SaveProgress.test.cs
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using lumina.Controllers;
using ServiceLayer.Exam.ExamAttempt;
using DataLayer.DTOs.Exam;
using DataLayer.DTOs.UserAnswer;
using System;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class SaveProgressTests
    {
        private readonly Mock<IExamAttemptService> _mockExamAttemptService;
        private readonly Mock<ILogger<ExamAttemptController>> _mockLogger;
        private readonly ExamAttemptController _controller;

        public SaveProgressTests()
        {
            _mockExamAttemptService = new Mock<IExamAttemptService>();
            _mockLogger = new Mock<ILogger<ExamAttemptController>>();
            _controller = new ExamAttemptController(_mockExamAttemptService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task SaveProgress_KhiRequestHopLeVaServiceThanhCong_TraVeOkVoiResponse()
        {
            // Arrange
            var request = new SaveProgressRequestDTO
            {
                ExamAttemptId = 1,
                CurrentQuestionIndex = 10
            };

            var expectedResponse = new SaveProgressResponseDTO
            {
                Success = true,
                Message = "Progress saved successfully"
            };

            _mockExamAttemptService
                .Setup(s => s.SaveProgressAsync(request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SaveProgress(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<SaveProgressResponseDTO>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Progress saved successfully", response.Message);
            _mockExamAttemptService.Verify(s => s.SaveProgressAsync(request), Times.Once);
        }

        [Fact]
        public async Task SaveProgress_KhiServiceTraVeSuccessFalse_TraVeBadRequest()
        {
            // Arrange
            var request = new SaveProgressRequestDTO
            {
                ExamAttemptId = 999,
                CurrentQuestionIndex = 5
            };

            var failedResponse = new SaveProgressResponseDTO
            {
                Success = false,
                Message = "Exam attempt not found"
            };

            _mockExamAttemptService
                .Setup(s => s.SaveProgressAsync(request))
                .ReturnsAsync(failedResponse);

            // Act
            var result = await _controller.SaveProgress(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<SaveProgressResponseDTO>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Exam attempt not found", response.Message);
            _mockExamAttemptService.Verify(s => s.SaveProgressAsync(request), Times.Once);
        }

        [Fact]
        public async Task SaveProgress_KhiModelStateInvalid_TraVeBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("ExamAttemptId", "Required");
            var request = new SaveProgressRequestDTO();

            // Act
            var result = await _controller.SaveProgress(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
            _mockExamAttemptService.Verify(s => s.SaveProgressAsync(It.IsAny<SaveProgressRequestDTO>()), Times.Never);
        }

        [Fact]
        public async Task SaveProgress_KhiServiceThrowException_TraVeInternalServerError()
        {
            // Arrange
            var request = new SaveProgressRequestDTO
            {
                ExamAttemptId = 1,
                CurrentQuestionIndex = 5
            };

            _mockExamAttemptService
                .Setup(s => s.SaveProgressAsync(request))
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            var result = await _controller.SaveProgress(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An unexpected error occurred while saving progress.", statusCodeResult.Value);
            _mockLogger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public async Task SaveProgress_KhiExamAttemptIdKhongHopLe_VaServiceTraVeFalse_TraVeBadRequest(int invalidId)
        {
            // Arrange
            var request = new SaveProgressRequestDTO
            {
                ExamAttemptId = invalidId,
                CurrentQuestionIndex = 5
            };

            var failedResponse = new SaveProgressResponseDTO
            {
                Success = false,
                Message = "Invalid exam attempt ID"
            };

            _mockExamAttemptService
                .Setup(s => s.SaveProgressAsync(request))
                .ReturnsAsync(failedResponse);

            // Act
            var result = await _controller.SaveProgress(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<SaveProgressResponseDTO>(badRequestResult.Value);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task SaveProgress_KhiCurrentQuestionIndexBangKhong_VaServiceThanhCong_TraVeOk()
        {
            // Arrange
            var request = new SaveProgressRequestDTO
            {
                ExamAttemptId = 1,
                CurrentQuestionIndex = 0
            };

            var expectedResponse = new SaveProgressResponseDTO
            {
                Success = true,
                Message = "Progress saved at start"
            };

            _mockExamAttemptService
                .Setup(s => s.SaveProgressAsync(request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SaveProgress(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<SaveProgressResponseDTO>(okResult.Value);
            Assert.True(response.Success);
        }

        [Fact]
        public async Task SaveProgress_KhiCurrentQuestionIndexNull_VaServiceThanhCong_TraVeOk()
        {
            // Arrange
            var request = new SaveProgressRequestDTO
            {
                ExamAttemptId = 1,
                CurrentQuestionIndex = null
            };

            var expectedResponse = new SaveProgressResponseDTO
            {
                Success = true,
                Message = "Progress saved without question index"
            };

            _mockExamAttemptService
                .Setup(s => s.SaveProgressAsync(request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SaveProgress(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<SaveProgressResponseDTO>(okResult.Value);
            Assert.True(response.Success);
        }

        [Fact]
        public async Task SaveProgress_KhiServiceThrowInvalidOperationException_TraVeInternalServerError()
        {
            // Arrange
            var request = new SaveProgressRequestDTO
            {
                ExamAttemptId = 1,
                CurrentQuestionIndex = 10
            };

            _mockExamAttemptService
                .Setup(s => s.SaveProgressAsync(request))
                .ThrowsAsync(new InvalidOperationException("Exam attempt is already completed"));

            // Act
            var result = await _controller.SaveProgress(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An unexpected error occurred while saving progress.", statusCodeResult.Value);
        }
    }
}
