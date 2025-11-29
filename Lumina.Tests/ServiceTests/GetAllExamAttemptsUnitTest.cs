using Xunit;
using Moq;
using ServiceLayer.Exam.ExamAttempt;
using RepositoryLayer.Exam.ExamAttempt;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Streak;
using Microsoft.Extensions.Logging;
using DataLayer.DTOs.UserAnswer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class GetAllExamAttemptsUnitTest
    {
        private readonly Mock<IExamAttemptRepository> _mockExamAttemptRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IStreakService> _mockStreakService;
        private readonly Mock<ILogger<ExamAttemptService>> _mockLogger;
        private readonly ExamAttemptService _service;

        public GetAllExamAttemptsUnitTest()
        {
            _mockExamAttemptRepository = new Mock<IExamAttemptRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockStreakService = new Mock<IStreakService>();
            _mockLogger = new Mock<ILogger<ExamAttemptService>>();

            _service = new ExamAttemptService(
                _mockExamAttemptRepository.Object,
                _mockUnitOfWork.Object,
                _mockStreakService.Object,
                _mockLogger.Object
            );
        }

        #region Test Cases for Invalid UserId

        [Fact]
        public async Task GetAllExamAttempts_WhenUserIdIsNegative_ShouldThrowArgumentException()
        {
            // Arrange
            int userId = -1;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetAllExamAttempts(userId)
            );

            Assert.Equal("userId", exception.ParamName);
            Assert.Contains("User ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.GetAllExamAttempts(It.IsAny<int>()),
                Times.Never
            );
        }

        [Fact]
        public async Task GetAllExamAttempts_WhenUserIdIsZero_ShouldThrowArgumentException()
        {
            // Arrange
            int userId = 0;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetAllExamAttempts(userId)
            );

            Assert.Equal("userId", exception.ParamName);
            Assert.Contains("User ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.GetAllExamAttempts(It.IsAny<int>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Valid UserId

        [Fact]
        public async Task GetAllExamAttempts_WhenUserIdIsValidAndHasData_ShouldReturnList()
        {
            // Arrange
            int userId = 1;
            var expectedResult = new List<ExamAttemptResponseDTO>
            {
                new ExamAttemptResponseDTO
                {
                    AttemptID = 1,
                    UserName = "TestUser",
                    ExamName = "Test Exam",
                    ExamPartName = "Part 1",
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow,
                    Score = 100,
                    Status = "Completed",
                    IsMocktest = false
                },
                new ExamAttemptResponseDTO
                {
                    AttemptID = 2,
                    UserName = "TestUser",
                    ExamName = "Test Exam 2",
                    ExamPartName = "Part 2",
                    StartTime = DateTime.UtcNow.AddDays(-1),
                    EndTime = DateTime.UtcNow.AddDays(-1),
                    Score = 85,
                    Status = "Completed",
                    IsMocktest = true
                }
            };

            _mockExamAttemptRepository
                .Setup(repo => repo.GetAllExamAttempts(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllExamAttempts(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(expectedResult[0].AttemptID, result[0].AttemptID);
            Assert.Equal(expectedResult[0].UserName, result[0].UserName);
            Assert.Equal(expectedResult[1].AttemptID, result[1].AttemptID);

            // Verify repository is called exactly once with correct userId
            _mockExamAttemptRepository.Verify(
                repo => repo.GetAllExamAttempts(userId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExamAttempts_WhenUserIdIsValidButNoData_ShouldReturnEmptyList()
        {
            // Arrange
            int userId = 1;
            var expectedResult = new List<ExamAttemptResponseDTO>();

            _mockExamAttemptRepository
                .Setup(repo => repo.GetAllExamAttempts(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllExamAttempts(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            // Verify repository is called exactly once
            _mockExamAttemptRepository.Verify(
                repo => repo.GetAllExamAttempts(userId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllExamAttempts_WhenUserIdIsLarge_ShouldReturnList()
        {
            // Arrange
            int userId = int.MaxValue;
            var expectedResult = new List<ExamAttemptResponseDTO>
            {
                new ExamAttemptResponseDTO
                {
                    AttemptID = 999,
                    UserName = "LargeUserId",
                    ExamName = "Test Exam",
                    ExamPartName = "Part 1",
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow,
                    Score = 90,
                    Status = "Completed",
                    IsMocktest = false
                }
            };

            _mockExamAttemptRepository
                .Setup(repo => repo.GetAllExamAttempts(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllExamAttempts(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(expectedResult[0].AttemptID, result[0].AttemptID);

            // Verify repository is called exactly once
            _mockExamAttemptRepository.Verify(
                repo => repo.GetAllExamAttempts(userId),
                Times.Once
            );
        }

        #endregion
    }
}

