using Xunit;
using Moq;
using ServiceLayer.Exam.ExamAttempt;
using RepositoryLayer.Exam.ExamAttempt;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.Generic;
using ServiceLayer.Streak;
using Microsoft.Extensions.Logging;
using DataLayer.DTOs.Exam;
using DataLayer.DTOs.UserAnswer;
using DataLayer.Models;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class SaveProgressAsyncUnitTest
    {
        private readonly Mock<IExamAttemptRepository> _mockExamAttemptRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IStreakService> _mockStreakService;
        private readonly Mock<ILogger<ExamAttemptService>> _mockLogger;
        private readonly Mock<IRepository<ExamAttempt>> _mockExamAttemptsGeneric;
        private readonly ExamAttemptService _service;

        public SaveProgressAsyncUnitTest()
        {
            _mockExamAttemptRepository = new Mock<IExamAttemptRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockStreakService = new Mock<IStreakService>();
            _mockLogger = new Mock<ILogger<ExamAttemptService>>();
            _mockExamAttemptsGeneric = new Mock<IRepository<ExamAttempt>>();

            _mockUnitOfWork.Setup(u => u.ExamAttemptsGeneric).Returns(_mockExamAttemptsGeneric.Object);

            _service = new ExamAttemptService(
                _mockExamAttemptRepository.Object,
                _mockUnitOfWork.Object,
                _mockStreakService.Object,
                _mockLogger.Object
            );
        }

        #region Test Cases for Null Request

        [Fact]
        public async Task SaveProgressAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            SaveProgressRequestDTO? request = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.SaveProgressAsync(request!)
            );

            Assert.Equal("request", exception.ParamName);
            Assert.Contains("Request cannot be null", exception.Message);

            // Verify repository is never called
            _mockExamAttemptsGeneric.Verify(
                repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Invalid ExamAttemptId

        [Fact]
        public async Task SaveProgressAsync_WhenExamAttemptIdIsNegative_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new SaveProgressRequestDTO
            {
                ExamAttemptId = -1,
                CurrentQuestionIndex = 0
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SaveProgressAsync(request)
            );

            Assert.Equal("ExamAttemptId", exception.ParamName);
            Assert.Contains("Exam Attempt ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockExamAttemptsGeneric.Verify(
                repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()),
                Times.Never
            );
        }

        [Fact]
        public async Task SaveProgressAsync_WhenExamAttemptIdIsZero_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new SaveProgressRequestDTO
            {
                ExamAttemptId = 0,
                CurrentQuestionIndex = 0
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SaveProgressAsync(request)
            );

            Assert.Equal("ExamAttemptId", exception.ParamName);
            Assert.Contains("Exam Attempt ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockExamAttemptsGeneric.Verify(
                repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Attempt Not Found

        [Fact]
        public async Task SaveProgressAsync_WhenAttemptNotFound_ShouldReturnSuccessFalse()
        {
            // Arrange
            var request = new SaveProgressRequestDTO
            {
                ExamAttemptId = 1,
                CurrentQuestionIndex = 5
            };

            ExamAttempt? attempt = null;

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(attempt);

            // Act
            var result = await _service.SaveProgressAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Exam attempt not found", result.Message);

            // Verify repository is called but Update and CompleteAsync are not called
            _mockExamAttemptsGeneric.Verify(
                repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()),
                Times.Once
            );
            _mockExamAttemptsGeneric.Verify(
                repo => repo.Update(It.IsAny<ExamAttempt>()),
                Times.Never
            );
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
        }

        #endregion

        #region Test Cases for Valid Request

        [Fact]
        public async Task SaveProgressAsync_WhenValid_ShouldReturnSuccessTrue()
        {
            // Arrange
            var request = new SaveProgressRequestDTO
            {
                ExamAttemptId = 1,
                CurrentQuestionIndex = 10
            };

            var attempt = new ExamAttempt
            {
                AttemptID = 1,
                UserID = 1,
                ExamID = 1,
                Status = "InProgress",
                StartTime = DateTime.UtcNow
            };

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(attempt);

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.SaveProgressAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Progress saved successfully", result.Message);
            Assert.Equal("Paused", attempt.Status);

            // Verify all methods are called
            _mockExamAttemptsGeneric.Verify(
                repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()),
                Times.Once
            );
            _mockExamAttemptsGeneric.Verify(
                repo => repo.Update(It.Is<ExamAttempt>(a => a.Status == "Paused" && a.AttemptID == request.ExamAttemptId)),
                Times.Once
            );
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task SaveProgressAsync_WhenValidWithNullCurrentQuestionIndex_ShouldReturnSuccessTrue()
        {
            // Arrange
            var request = new SaveProgressRequestDTO
            {
                ExamAttemptId = 1,
                CurrentQuestionIndex = null
            };

            var attempt = new ExamAttempt
            {
                AttemptID = 1,
                UserID = 1,
                ExamID = 1,
                Status = "InProgress",
                StartTime = DateTime.UtcNow
            };

            _mockExamAttemptsGeneric
                .Setup(repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(attempt);

            _mockUnitOfWork
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.SaveProgressAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Progress saved successfully", result.Message);

            // Verify all methods are called
            _mockExamAttemptsGeneric.Verify(
                repo => repo.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ExamAttempt, bool>>>(), It.IsAny<string>()),
                Times.Once
            );
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        #endregion
    }
}

