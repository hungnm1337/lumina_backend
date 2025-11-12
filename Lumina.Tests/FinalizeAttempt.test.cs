// File: FinalizeAttempt.test.cs
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using lumina.Controllers;
using ServiceLayer.Exam.ExamAttempt;
using DataLayer.DTOs.Exam;
using DataLayer.DTOs.UserAnswer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class FinalizeAttemptTests
    {
        private readonly Mock<IExamAttemptService> _mockExamAttemptService;
        private readonly Mock<ILogger<ExamAttemptController>> _mockLogger;
        private readonly ExamAttemptController _controller;

        public FinalizeAttemptTests()
        {
            _mockExamAttemptService = new Mock<IExamAttemptService>();
            _mockLogger = new Mock<ILogger<ExamAttemptController>>();
            _controller = new ExamAttemptController(_mockExamAttemptService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task FinalizeAttempt_KhiRequestHopLeVaServiceThanhCong_TraVeOkVoiSummary()
        {
            // Arrange
            var request = new FinalizeAttemptRequestDTO
            {
                ExamAttemptId = 1
            };

            var expectedSummary = new ExamAttemptSummaryDTO
            {
                Success = true,
                ExamAttemptId = 1,
                TotalScore = 850,
                TotalQuestions = 200,
                CorrectAnswers = 170,
                IncorrectAnswers = 30,
                PercentCorrect = 85.0,
                StartTime = DateTime.UtcNow.AddHours(-2),
                EndTime = DateTime.UtcNow,
                Duration = TimeSpan.FromHours(2),
                Answers = new List<UserAnswerDetailDTO>()
            };

            _mockExamAttemptService
                .Setup(s => s.FinalizeAttemptAsync(request.ExamAttemptId))
                .ReturnsAsync(expectedSummary);

            // Act
            var result = await _controller.FinalizeAttempt(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var summary = Assert.IsType<ExamAttemptSummaryDTO>(okResult.Value);
            Assert.True(summary.Success);
            Assert.Equal(1, summary.ExamAttemptId);
            Assert.Equal(850, summary.TotalScore);
            Assert.Equal(200, summary.TotalQuestions);
            Assert.Equal(170, summary.CorrectAnswers);
            Assert.Equal(85.0, summary.PercentCorrect);
            _mockExamAttemptService.Verify(s => s.FinalizeAttemptAsync(request.ExamAttemptId), Times.Once);
        }

        [Fact]
        public async Task FinalizeAttempt_KhiServiceTraVeSuccessFalse_TraVeBadRequest()
        {
            // Arrange
            var request = new FinalizeAttemptRequestDTO
            {
                ExamAttemptId = 999
            };

            var failedSummary = new ExamAttemptSummaryDTO
            {
                Success = false,
                ExamAttemptId = 999,
                TotalScore = 0,
                TotalQuestions = 0,
                CorrectAnswers = 0,
                IncorrectAnswers = 0,
                PercentCorrect = 0
            };

            _mockExamAttemptService
                .Setup(s => s.FinalizeAttemptAsync(request.ExamAttemptId))
                .ReturnsAsync(failedSummary);

            // Act
            var result = await _controller.FinalizeAttempt(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic value = badRequestResult.Value!;
            Assert.Equal("Failed to finalize exam attempt", value.message);
            _mockExamAttemptService.Verify(s => s.FinalizeAttemptAsync(request.ExamAttemptId), Times.Once);
        }

        [Fact]
        public async Task FinalizeAttempt_KhiModelStateInvalid_TraVeBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("ExamAttemptId", "Required");
            var request = new FinalizeAttemptRequestDTO();

            // Act
            var result = await _controller.FinalizeAttempt(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
            _mockExamAttemptService.Verify(s => s.FinalizeAttemptAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task FinalizeAttempt_KhiServiceThrowException_TraVeInternalServerError()
        {
            // Arrange
            var request = new FinalizeAttemptRequestDTO
            {
                ExamAttemptId = 1
            };

            _mockExamAttemptService
                .Setup(s => s.FinalizeAttemptAsync(request.ExamAttemptId))
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            var result = await _controller.FinalizeAttempt(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An unexpected error occurred while finalizing the exam.", statusCodeResult.Value);
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
        public async Task FinalizeAttempt_KhiExamAttemptIdKhongHopLe_VaServiceTraVeFalse_TraVeBadRequest(int invalidId)
        {
            // Arrange
            var request = new FinalizeAttemptRequestDTO
            {
                ExamAttemptId = invalidId
            };

            var failedSummary = new ExamAttemptSummaryDTO
            {
                Success = false,
                ExamAttemptId = invalidId
            };

            _mockExamAttemptService
                .Setup(s => s.FinalizeAttemptAsync(invalidId))
                .ReturnsAsync(failedSummary);

            // Act
            var result = await _controller.FinalizeAttempt(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            dynamic value = badRequestResult.Value!;
            Assert.Equal("Failed to finalize exam attempt", value.message);
        }

        [Fact]
        public async Task FinalizeAttempt_KhiExamAttemptIdLonNhat_VaServiceThanhCong_TraVeOk()
        {
            // Arrange
            var request = new FinalizeAttemptRequestDTO
            {
                ExamAttemptId = int.MaxValue
            };

            var expectedSummary = new ExamAttemptSummaryDTO
            {
                Success = true,
                ExamAttemptId = int.MaxValue,
                TotalScore = 990,
                TotalQuestions = 200,
                CorrectAnswers = 198,
                IncorrectAnswers = 2,
                PercentCorrect = 99.0,
                StartTime = DateTime.UtcNow.AddHours(-2),
                EndTime = DateTime.UtcNow,
                Duration = TimeSpan.FromHours(2),
                Answers = new List<UserAnswerDetailDTO>()
            };

            _mockExamAttemptService
                .Setup(s => s.FinalizeAttemptAsync(int.MaxValue))
                .ReturnsAsync(expectedSummary);

            // Act
            var result = await _controller.FinalizeAttempt(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var summary = Assert.IsType<ExamAttemptSummaryDTO>(okResult.Value);
            Assert.True(summary.Success);
            Assert.Equal(int.MaxValue, summary.ExamAttemptId);
        }

        [Fact]
        public async Task FinalizeAttempt_KhiServiceThrowInvalidOperationException_TraVeInternalServerError()
        {
            // Arrange
            var request = new FinalizeAttemptRequestDTO
            {
                ExamAttemptId = 1
            };

            _mockExamAttemptService
                .Setup(s => s.FinalizeAttemptAsync(request.ExamAttemptId))
                .ThrowsAsync(new InvalidOperationException("Exam attempt already finalized"));

            // Act
            var result = await _controller.FinalizeAttempt(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An unexpected error occurred while finalizing the exam.", statusCodeResult.Value);
        }

        [Fact]
        public async Task FinalizeAttempt_KhiServiceThrowKeyNotFoundException_TraVeInternalServerError()
        {
            // Arrange
            var request = new FinalizeAttemptRequestDTO
            {
                ExamAttemptId = 999
            };

            _mockExamAttemptService
                .Setup(s => s.FinalizeAttemptAsync(request.ExamAttemptId))
                .ThrowsAsync(new KeyNotFoundException("Exam attempt not found"));

            // Act
            var result = await _controller.FinalizeAttempt(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An unexpected error occurred while finalizing the exam.", statusCodeResult.Value);
        }
    }
}
