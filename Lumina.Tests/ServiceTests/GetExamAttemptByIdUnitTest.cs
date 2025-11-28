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
    public class GetExamAttemptByIdUnitTest
    {
        private readonly Mock<IExamAttemptRepository> _mockExamAttemptRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IStreakService> _mockStreakService;
        private readonly Mock<ILogger<ExamAttemptService>> _mockLogger;
        private readonly ExamAttemptService _service;

        public GetExamAttemptByIdUnitTest()
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

        #region Test Cases for Invalid AttemptId

        [Fact]
        public async Task GetExamAttemptById_WhenAttemptIdIsNegative_ShouldThrowArgumentException()
        {
            // Arrange
            int attemptId = -1;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetExamAttemptById(attemptId)
            );

            Assert.Equal("attemptId", exception.ParamName);
            Assert.Contains("Attempt ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.GetExamAttemptById(It.IsAny<int>()),
                Times.Never
            );
        }

        [Fact]
        public async Task GetExamAttemptById_WhenAttemptIdIsZero_ShouldThrowArgumentException()
        {
            // Arrange
            int attemptId = 0;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetExamAttemptById(attemptId)
            );

            Assert.Equal("attemptId", exception.ParamName);
            Assert.Contains("Attempt ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockExamAttemptRepository.Verify(
                repo => repo.GetExamAttemptById(It.IsAny<int>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Valid AttemptId

        [Fact]
        public async Task GetExamAttemptById_WhenAttemptIdIsValidAndExists_ShouldReturnDTO()
        {
            // Arrange
            int attemptId = 1;
            var expectedResult = new ExamAttemptDetailResponseDTO
            {
                ExamAttemptInfo = new ExamAttemptResponseDTO
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
                ListeningAnswers = new List<ListeningAnswerResponseDTO>(),
                SpeakingAnswers = new List<SpeakingAnswerResponseDTO>(),
                ReadingAnswers = new List<ReadingAnswerResponseDTO>(),
                WritingAnswers = new List<WritingAnswerResponseDTO>()
            };

            _mockExamAttemptRepository
                .Setup(repo => repo.GetExamAttemptById(attemptId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetExamAttemptById(attemptId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ExamAttemptInfo);
            Assert.Equal(expectedResult.ExamAttemptInfo.AttemptID, result.ExamAttemptInfo.AttemptID);
            Assert.Equal(expectedResult.ExamAttemptInfo.UserName, result.ExamAttemptInfo.UserName);
            Assert.Equal(expectedResult.ExamAttemptInfo.Status, result.ExamAttemptInfo.Status);

            // Verify repository is called exactly once with correct attemptId
            _mockExamAttemptRepository.Verify(
                repo => repo.GetExamAttemptById(attemptId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetExamAttemptById_WhenAttemptIdIsValidButNotFound_ShouldReturnNull()
        {
            // Arrange
            int attemptId = 999;

            _mockExamAttemptRepository
                .Setup(repo => repo.GetExamAttemptById(attemptId))
                .ReturnsAsync((ExamAttemptDetailResponseDTO?)null);

            // Act
            var result = await _service.GetExamAttemptById(attemptId);

            // Assert
            Assert.Null(result);

            // Verify repository is called exactly once
            _mockExamAttemptRepository.Verify(
                repo => repo.GetExamAttemptById(attemptId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetExamAttemptById_WhenAttemptIdIsLarge_ShouldReturnDTO()
        {
            // Arrange
            int attemptId = int.MaxValue;
            var expectedResult = new ExamAttemptDetailResponseDTO
            {
                ExamAttemptInfo = new ExamAttemptResponseDTO
                {
                    AttemptID = int.MaxValue,
                    UserName = "LargeAttemptId",
                    ExamName = "Test Exam",
                    ExamPartName = "Part 1",
                    StartTime = DateTime.UtcNow,
                    EndTime = null,
                    Score = null,
                    Status = "InProgress",
                    IsMocktest = false
                },
                ListeningAnswers = null,
                SpeakingAnswers = null,
                ReadingAnswers = null,
                WritingAnswers = null
            };

            _mockExamAttemptRepository
                .Setup(repo => repo.GetExamAttemptById(attemptId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetExamAttemptById(attemptId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ExamAttemptInfo);
            Assert.Equal(int.MaxValue, result.ExamAttemptInfo.AttemptID);

            // Verify repository is called exactly once
            _mockExamAttemptRepository.Verify(
                repo => repo.GetExamAttemptById(attemptId),
                Times.Once
            );
        }

        #endregion
    }
}

